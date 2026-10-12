using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Domain;

public class BackupFormatTests
{
    private static BackupDocuments Ok(BackupReadResult result) => result switch
    {
        BackupDocuments documents => documents,
        BackupError error => throw new Xunit.Sdk.XunitException($"Expected documents, got {error.Kind}"),
    };

    private static BackupErrorKind Error(BackupReadResult result) => result switch
    {
        BackupDocuments => throw new Xunit.Sdk.XunitException("Expected an error"),
        BackupError error => error.Kind,
    };

    [Theory]
    [InlineData("not json", BackupErrorKind.InvalidJson)]
    [InlineData("[1, 2]", BackupErrorKind.InvalidJson)]
    [InlineData("""{ "collection": "other", "documents": {} }""", BackupErrorKind.WrongCollection)]
    [InlineData("""{ "documents": {} }""", BackupErrorKind.WrongCollection)]
    [InlineData("""{ "collection": "pokemon" }""", BackupErrorKind.MissingDocuments)]
    public void Rejects_invalid_files(string json, BackupErrorKind expected) =>
        Assert.Equal(expected, Error(BackupFormat.Read(json)));

    [Fact]
    public void Read_of_Write_is_identity()
    {
        var a = Pokemons.Squirtle().WithEvents(("Bulbasaur (rival)", 1, 1), ("Caterpie", 3, 2), ("Nidoran F", 1, 1));
        a.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 11], Evs = new int[6] });
        a.Pokerus = true;
        var b = Pokemons.Squirtle(nature: "");
        b.Order = 2;
        var all = new Dictionary<string, TrainingPokemon> { ["a"] = a, ["b"] = b };

        var json = BackupFormat.Write(all, indented: true);
        var back = Ok(BackupFormat.Read(json)).Items;

        Assert.Equal(json, BackupFormat.Write(back, indented: true));
    }

    [Fact]
    public void Manual_adjustments_and_known_ivs_round_trip()
    {
        var a = Pokemons.Squirtle().WithEvents(("Caterpie", 3, 1));
        a.Events.Add(new BattleEvent { Opponent = "", SetTotals = [12, 0, 2, 26, 0, 4] });
        a.WithEvents(("Abra", 1, 2));
        a.KnownIvs[5] = 30;
        a.KnownIvs[0] = 0;
        var all = new Dictionary<string, TrainingPokemon> { ["a"] = a };

        var json = BackupFormat.Write(all, indented: true);
        var back = Ok(BackupFormat.Read(json)).Items["a"];

        Assert.Equal([12, 0, 2, 26, 0, 4], back.Events[1].SetTotals!);
        Assert.Null(back.Events[0].SetTotals);
        Assert.Equal([0, null, null, null, null, 30], back.KnownIvs);
        Assert.Equal(json, BackupFormat.Write(new Dictionary<string, TrainingPokemon> { ["a"] = back }, indented: true));
    }

    [Fact]
    public void Exp_share_holder_and_shared_battles_round_trip_and_are_written_only_when_true()
    {
        var holder = Pokemons.Squirtle().WithEvents(("Caterpie", 2, 1));
        holder.ExpShare = true;
        holder.Events.Add(new BattleEvent { Opponent = "Caterpie", Count = 3, Multiplier = 2, Shared = true });
        var other = Pokemons.Squirtle().WithEvents(("Abra", 1, 1));
        var all = new Dictionary<string, TrainingPokemon> { ["h"] = holder, ["o"] = other };

        var json = BackupFormat.Write(all, indented: false);
        var back = Ok(BackupFormat.Read(json)).Items;

        Assert.True(back["h"].ExpShare);
        Assert.False(back["o"].ExpShare);
        Assert.Equal([false, true], back["h"].Events.Select(e => e.Shared));
        Assert.Equal(1, json.Split("\"expShare\":true").Length - 1);
        Assert.DoesNotContain("\"expShare\":false", json);
        Assert.Equal(1, json.Split("\"x\":true").Length - 1);
        Assert.DoesNotContain("\"x\":false", json);
        Assert.Equal(json, BackupFormat.Write(back, indented: false));

        var clone = holder.Clone();
        Assert.True(clone.ExpShare);
        Assert.True(clone.Events[1].Shared);
    }

    [Fact]
    public void Old_backup_with_a_nickname_gets_the_current_species_and_keeps_everything_else()
    {
        const string json = """
            {
              "collection": "pokemon",
              "documents": {
                "g": {
                  "name": "Gardi",
                  "nature": "Modest",
                  "species": [
                    { "name": "Ralts", "base": [28, 25, 25, 45, 35, 40] },
                    { "name": "Kirlia", "base": [38, 35, 35, 65, 55, 50] },
                    { "name": "Gardevoir", "base": [68, 65, 65, 125, 115, 80] }
                  ],
                  "targets": [0, 0, 0, 252, 0, 252],
                  "events": [ { "o": "Abra", "n": 4, "m": 1 } ],
                  "ivLog": [
                    { "lv": 5, "sp": "Ralts", "st": [20, 9, 9, 13, 11, 12], "ev": [0, 0, 0, 0, 0, 0] },
                    { "lv": 20, "sp": "Kirlia", "st": [50, 25, 25, 41, 35, 32], "ev": [0, 0, 0, 4, 0, 0] }
                  ],
                  "pokerus": true,
                  "macho": false,
                  "order": 1
                }
              }
            }
            """;

        var back = Ok(BackupFormat.Read(json)).Items["g"];

        Assert.Equal("Kirlia", back.CurrentSpecies);
        Assert.Equal("Modest", back.Nature);
        Assert.Equal(["Ralts", "Kirlia", "Gardevoir"], back.Species.Select(s => s.Name));
        Assert.Equal([0, 0, 0, 252, 0, 252], back.Targets);
        Assert.Equal("Abra", Assert.Single(back.Events).Opponent);
        Assert.Equal([5, 20], back.IvLog.Select(l => l.Level));
        Assert.True(back.Pokerus);

        var written = BackupFormat.Write(new Dictionary<string, TrainingPokemon> { ["g"] = back }, indented: true);
        Assert.Contains("\"name\": \"Kirlia\"", written, StringComparison.Ordinal);
        Assert.Equal(written, BackupFormat.Write(Ok(BackupFormat.Read(written)).Items, indented: true));
    }

    [Fact]
    public void Missing_name_is_accepted()
    {
        const string json = """{ "collection": "pokemon", "documents": { "s": { "species": [ { "name": "Squirtle", "base": [44, 48, 65, 50, 64, 43] } ] } } }""";

        Assert.Equal("Squirtle", Ok(BackupFormat.Read(json)).Items["s"].CurrentSpecies);
    }

    [Fact]
    public void Write_keeps_alias_names_as_recorded()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Bulbasaur (rival)", 1, 1), ("Nidoran F", 1, 1));

        var json = BackupFormat.Write(new Dictionary<string, TrainingPokemon> { ["x"] = pokemon }, indented: true);

        Assert.Contains("\"Bulbasaur (rival)\"", json);
        Assert.Contains("\"Nidoran F\"", json);
    }

    [Fact]
    public void Preview_counts_new_and_replaced_by_id()
    {
        var current = new Dictionary<string, TrainingPokemon> { ["a"] = Pokemons.Squirtle(), ["c"] = Pokemons.Squirtle() };
        var incoming = new Dictionary<string, TrainingPokemon> { ["a"] = Pokemons.Squirtle(), ["b"] = Pokemons.Squirtle() };

        Assert.Equal(new BackupMergePreview(New: 1, Replaced: 1), BackupFormat.Preview(current, incoming));
    }

    [Fact]
    public void Updated_at_round_trips_and_old_files_stay_valid()
    {
        var a = Pokemons.Squirtle();
        a.UpdatedAt = new DateTimeOffset(2026, 10, 11, 21, 30, 0, TimeSpan.Zero);
        var all = new Dictionary<string, TrainingPokemon> { ["a"] = a, ["b"] = Pokemons.Squirtle() };

        var json = BackupFormat.Write(all, indented: true);
        var back = Ok(BackupFormat.Read(json)).Items;

        Assert.Equal(a.UpdatedAt, back["a"].UpdatedAt);
        Assert.Null(back["b"].UpdatedAt);
        Assert.Equal(json, BackupFormat.Write(back, indented: true));
    }
}