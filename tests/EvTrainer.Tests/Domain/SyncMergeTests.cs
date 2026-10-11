using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Domain;

/// <summary>Merge rule of contracts/sync.md: per Pokémon, the most recent event (version or deletion) wins.</summary>
public class SyncMergeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset At10 = Now.AddHours(-2);
    private static readonly DateTimeOffset At11 = Now.AddHours(-1);

    private static TrainingPokemon Version(DateTimeOffset? at, string species = "Squirtle")
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.CurrentSpecies = species;
        pokemon.UpdatedAt = at;
        return pokemon;
    }

    private static SyncSnapshot Snapshot(
        IEnumerable<(string Id, TrainingPokemon Pokemon)>? documents = null,
        IEnumerable<(string Id, DateTimeOffset At)>? deleted = null) =>
        new(
            (documents ?? []).ToDictionary(d => d.Id, d => d.Pokemon),
            (deleted ?? []).ToDictionary(d => d.Id, d => d.At));

    private static readonly SyncSnapshot Empty = Snapshot();

    private static MergeResult Merge(SyncSnapshot local, SyncSnapshot remote) => SyncMerge.Merge(local, remote, Now);

    [Fact]
    public void Only_local_is_kept_and_uploaded()
    {
        var result = Merge(Snapshot([("A", Version(At10))]), Empty);

        Assert.Equal(At10, result.Documents["A"].UpdatedAt);
        Assert.False(result.LocalChanged);
        Assert.True(result.RemoteChanged);
        Assert.Equal((0, 0, 0), (result.Updated, result.Added, result.Removed));
    }

    [Fact]
    public void Only_remote_is_added_locally()
    {
        var result = Merge(Empty, Snapshot([("A", Version(At10))]));

        Assert.Equal(At10, result.Documents["A"].UpdatedAt);
        Assert.True(result.LocalChanged);
        Assert.False(result.RemoteChanged);
        Assert.Equal(1, result.Added);
    }

    [Fact]
    public void Newer_remote_version_wins()
    {
        var result = Merge(Snapshot([("A", Version(At10))]), Snapshot([("A", Version(At11, "Wartortle"))]));

        Assert.Equal("Wartortle", result.Documents["A"].CurrentSpecies);
        Assert.Equal(1, result.Updated);
        Assert.True(result.LocalChanged);
        Assert.False(result.RemoteChanged);
    }

    [Fact]
    public void Newer_local_version_wins()
    {
        var result = Merge(Snapshot([("A", Version(At11, "Wartortle"))]), Snapshot([("A", Version(At10))]));

        Assert.Equal("Wartortle", result.Documents["A"].CurrentSpecies);
        Assert.Equal(0, result.Updated);
        Assert.False(result.LocalChanged);
        Assert.True(result.RemoteChanged);
    }

    [Fact]
    public void Newer_remote_deletion_removes_the_local_version()
    {
        var result = Merge(Snapshot([("A", Version(At10))]), Snapshot(deleted: [("A", At11)]));

        Assert.Empty(result.Documents);
        Assert.Equal(At11, result.Deleted["A"]);
        Assert.Equal(1, result.Removed);
        Assert.True(result.LocalChanged);
        Assert.False(result.RemoteChanged);
    }

    [Fact]
    public void Newer_local_deletion_is_uploaded()
    {
        var result = Merge(Snapshot(deleted: [("A", At11)]), Snapshot([("A", Version(At10))]));

        Assert.Empty(result.Documents);
        Assert.Equal(At11, result.Deleted["A"]);
        Assert.False(result.LocalChanged);
        Assert.True(result.RemoteChanged);
    }

    [Fact]
    public void Edit_after_the_deletion_keeps_the_pokemon()
    {
        var result = Merge(Snapshot(deleted: [("A", At10)]), Snapshot([("A", Version(At11))]));

        Assert.Equal(At11, result.Documents["A"].UpdatedAt);
        Assert.Empty(result.Deleted);
        Assert.Equal(1, result.Added);
        Assert.True(result.LocalChanged);
        Assert.False(result.RemoteChanged);
    }

    [Fact]
    public void Undated_version_loses_to_a_dated_one()
    {
        var result = Merge(Snapshot([("A", Version(null, "Wartortle"))]), Snapshot([("A", Version(At10))]));

        Assert.Equal("Squirtle", result.Documents["A"].CurrentSpecies);
        Assert.Equal(1, result.Updated);
    }

    [Fact]
    public void Same_time_different_content_picks_the_same_winner_on_both_sides()
    {
        var left = Snapshot([("A", Version(At10, "Squirtle"))]);
        var right = Snapshot([("A", Version(At10, "Wartortle"))]);

        var fromLeft = Merge(left, right).Documents["A"].CurrentSpecies;
        var fromRight = Merge(right, left).Documents["A"].CurrentSpecies;

        Assert.Equal(fromLeft, fromRight);
    }

    [Fact]
    public void Same_version_on_both_sides_changes_nothing()
    {
        var result = Merge(Snapshot([("A", Version(At10))]), Snapshot([("A", Version(At10))]));

        Assert.False(result.LocalChanged);
        Assert.False(result.RemoteChanged);
        Assert.Equal((0, 0, 0), (result.Updated, result.Added, result.Removed));
    }

    [Fact]
    public void Deletions_older_than_90_days_are_dropped()
    {
        var old = Now - SyncMerge.DeletionRetention - TimeSpan.FromDays(1);
        var recent = Now - TimeSpan.FromDays(89);

        var result = Merge(Snapshot(deleted: [("Old", old), ("Recent", recent)]), Snapshot(deleted: [("Old", old)]));

        Assert.Equal(["Recent"], result.Deleted.Keys);
    }

    [Fact]
    public void Merging_again_with_the_same_remote_is_a_no_op()
    {
        var local = Snapshot([("A", Version(At11, "Wartortle")), ("B", Version(At10))], [("C", At10)]);
        var remote = Snapshot([("A", Version(At10)), ("D", Version(At11))], [("B", At11)]);

        var first = Merge(local, remote);
        var merged = new SyncSnapshot(first.Documents, first.Deleted);
        var again = Merge(merged, remote);
        var otherSide = Merge(remote, merged);

        Assert.False(again.LocalChanged);
        Assert.Equal(
            first.Documents.ToDictionary(kv => kv.Key, kv => Json(kv.Value)),
            again.Documents.ToDictionary(kv => kv.Key, kv => Json(kv.Value)));
        Assert.False(Merge(merged, merged).RemoteChanged);
        Assert.Equal(first.Documents.Keys.Order(), otherSide.Documents.Keys.Order());
        Assert.Equal(first.Deleted, otherSide.Deleted);
    }

    private static string Json(TrainingPokemon pokemon) =>
        PokemonJson.Serialize(new Dictionary<string, TrainingPokemon> { ["x"] = pokemon }, indented: false);
}