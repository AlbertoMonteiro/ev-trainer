using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.Domain;

/// <summary>Everything a device syncs: the Pokémon and the deletion records (spec 008).</summary>
public sealed record SyncSnapshot(
    IReadOnlyDictionary<string, TrainingPokemon> Documents,
    IReadOnlyDictionary<string, DateTimeOffset> Deleted);

/// <param name="Updated">Local Pokémon replaced by a newer remote version.</param>
/// <param name="Added">Pokémon that exist locally only because of the remote side.</param>
/// <param name="Removed">Local Pokémon removed by a newer remote deletion.</param>
/// <param name="LocalChanged">The local data must be replaced by the result.</param>
/// <param name="RemoteChanged">The remote copy must be rewritten with the result.</param>
public sealed record MergeResult(
    Dictionary<string, TrainingPokemon> Documents,
    Dictionary<string, DateTimeOffset> Deleted,
    int Updated,
    int Added,
    int Removed,
    bool LocalChanged,
    bool RemoteChanged);

/// <summary>
/// Merge rule of the sync (research R4): per Pokémon, the most recent event — a version (its
/// <see cref="TrainingPokemon.UpdatedAt"/>) or a deletion — wins. The result does not depend on which side is
/// local, and merging again with the same remote changes nothing, so devices converge in any order.
/// </summary>
public static class SyncMerge
{
    public static readonly TimeSpan DeletionRetention = TimeSpan.FromDays(90);

    private enum Side
    {
        Local,
        Remote,
    }

    private sealed record Candidate(Side Side, DateTimeOffset At, TrainingPokemon? Version, string? Json);

    public static MergeResult Merge(SyncSnapshot local, SyncSnapshot remote, DateTimeOffset now)
    {
        var documents = new Dictionary<string, TrainingPokemon>(StringComparer.Ordinal);
        var deleted = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        int updated = 0, added = 0, removed = 0;
        bool localChanged = false, remoteChanged = false;
        var oldest = now - DeletionRetention;

        var ids = local.Documents.Keys.Concat(local.Deleted.Keys).Concat(remote.Documents.Keys).Concat(remote.Deleted.Keys)
            .Distinct(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            var localVersion = VersionOf(local, id, Side.Local);
            var remoteVersion = VersionOf(remote, id, Side.Remote);
            var deletedAt = Max(local.Deleted.GetValueOrDefault(id), remote.Deleted.GetValueOrDefault(id));
            var newest = Newest(localVersion, remoteVersion);

            if (newest is null || deletedAt >= newest.At)
            {
                // The deletion is the most recent event (a tie also deletes); old records are dropped.
                var at = deletedAt!.Value;
                DateTimeOffset? record = at >= oldest ? at : null;
                if (record is { } kept)
                {
                    deleted[id] = kept;
                }

                if (localVersion is not null)
                {
                    removed++;
                    localChanged = true;
                }
                else if (RecordOf(local, id) != record)
                {
                    localChanged = true;
                }

                if (remoteVersion is not null || RecordOf(remote, id) != record)
                {
                    remoteChanged = true;
                }

                continue;
            }

            documents[id] = newest.Version!.Clone();
            if (!SameVersion(localVersion, newest) || local.Deleted.ContainsKey(id))
            {
                localChanged = true;
                if (localVersion is null)
                {
                    added++;
                }
                else
                {
                    updated++;
                }
            }

            if (!SameVersion(remoteVersion, newest) || remote.Deleted.ContainsKey(id))
            {
                remoteChanged = true;
            }
        }

        return new MergeResult(documents, deleted, updated, added, removed, localChanged, remoteChanged);
    }

    private static DateTimeOffset? RecordOf(SyncSnapshot snapshot, string id) =>
        snapshot.Deleted.TryGetValue(id, out var at) ? at : null;

    private static Candidate? VersionOf(SyncSnapshot snapshot, string id, Side side) =>
        snapshot.Documents.TryGetValue(id, out var pokemon)
            ? new Candidate(side, pokemon.UpdatedAt ?? DateTimeOffset.MinValue, pokemon, Serialize(pokemon))
            : null;

    /// <summary>The later version; on the same instant, the larger content decides so both sides agree.</summary>
    private static Candidate? Newest(Candidate? a, Candidate? b) => (a, b) switch
    {
        (null, _) => b,
        (_, null) => a,
        _ when a.At != b.At => a.At > b.At ? a : b,
        _ => string.CompareOrdinal(a.Json, b.Json) >= 0 ? a : b,
    };

    private static bool SameVersion(Candidate? side, Candidate winner) =>
        side is not null && side.At == winner.At && side.Json == winner.Json;

    private static DateTimeOffset? Max(DateTimeOffset a, DateTimeOffset b)
    {
        var max = a > b ? a : b;
        return max == default ? null : max;
    }

    private static string Serialize(TrainingPokemon pokemon) =>
        PokemonJson.Serialize(new Dictionary<string, TrainingPokemon> { ["x"] = pokemon }, indented: false);
}