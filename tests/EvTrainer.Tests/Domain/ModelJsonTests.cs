using System.Text.Json.Nodes;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Domain;

public class ModelJsonTests
{
    [Fact]
    public void Writes_handoff_property_names()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 8, 1));
        pokemon.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 11], Evs = new int[6] });

        var json = PokemonJson.Serialize(new Dictionary<string, TrainingPokemon> { ["sq"] = pokemon }, indented: false);
        var root = JsonNode.Parse(json)!;
        var doc = root["documents"]!["sq"]!.AsObject();

        Assert.Equal("pokemon", root["collection"]!.GetValue<string>());
        Assert.Equal(
            ["events", "ivLog", "knownIvs", "macho", "name", "nature", "order", "pokerus", "species", "targets"],
            doc.Select(p => p.Key).Order(StringComparer.Ordinal));
        Assert.NotNull(doc["species"]![0]!["base"]);
        Assert.Equal("Caterpie", doc["events"]![0]!["o"]!.GetValue<string>());
        Assert.Equal(8, doc["events"]![0]!["n"]!.GetValue<int>());
        Assert.Equal(1, doc["events"]![0]!["m"]!.GetValue<int>());
        var level = doc["ivLog"]![0]!;
        Assert.Equal(5, level["lv"]!.GetValue<int>());
        Assert.Equal("Squirtle", level["sp"]!.GetValue<string>());
        Assert.Equal(6, level["st"]!.AsArray().Count);
        Assert.Equal(6, level["ev"]!.AsArray().Count);
    }

    [Fact]
    public void Name_holds_the_current_species()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.CurrentSpecies = "Wartortle";

        var json = PokemonJson.Serialize(new Dictionary<string, TrainingPokemon> { ["sq"] = pokemon }, indented: false);

        Assert.Equal("Wartortle", JsonNode.Parse(json)!["documents"]!["sq"]!["name"]!.GetValue<string>());
        Assert.Equal("Wartortle", PokemonJson.Deserialize(json)!["sq"].CurrentSpecies);
    }

    [Fact]
    public void Round_trips_without_changes()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Bulbasaur (rival)", 1, 1), ("Caterpie", 3, 2));
        pokemon.Pokerus = true;
        var all = new Dictionary<string, TrainingPokemon> { ["a"] = pokemon };

        var first = PokemonJson.Serialize(all, indented: true);
        var again = PokemonJson.Serialize(PokemonJson.Deserialize(first)!, indented: true);

        Assert.Equal(first, again);
    }

    [Fact]
    public void Reads_tolerantly()
    {
        const string json = """
            {
              "collection": "pokemon",
              "extra": true,
              "documents": {
                "x": {
                  "name": "X",
                  "unknown": 123,
                  "species": [ { "name": "A", "base": [1, 2, 3] } ],
                  "targets": [1, 2, 3, 4, 5, 6, 7],
                  "events": [ { "o": "Caterpie" }, { "o": "Weedle", "n": 0, "m": 3 }, { "o": "Pidgey", "n": 2, "m": 4 } ],
                  "ivLog": [
                    { "lv": 9, "sp": "A", "st": [1, 1, 1, 1, 1, 1] },
                    { "lv": 5, "sp": "A", "st": [1, 1, 1, 1, 1, 1], "ev": [0, 0, 0, 0, 0, 0] }
                  ]
                }
              }
            }
            """;

        var x = PokemonJson.Deserialize(json)!["x"];

        Assert.Equal("", x.Nature);
        Assert.Equal([1, 2, 3, 0, 0, 0], x.Species[0].Base);
        Assert.Equal([1, 2, 3, 4, 5, 6], x.Targets);
        Assert.Equal((1, 1), (x.Events[0].Count, x.Events[0].Multiplier));
        Assert.Equal((1, 1), (x.Events[1].Count, x.Events[1].Multiplier));
        Assert.Equal((2, 4), (x.Events[2].Count, x.Events[2].Multiplier));
        Assert.Equal([5, 9], x.IvLog.Select(l => l.Level));
        Assert.Equal(new int[6], x.IvLog[1].Evs);
        Assert.Equal(1, x.Order);
    }

    [Fact]
    public void Empty_species_list_becomes_one_unnamed_species()
    {
        const string json = """{ "collection": "pokemon", "documents": { "x": { "name": "X", "species": [] } } }""";

        var x = PokemonJson.Deserialize(json)!["x"];

        var species = Assert.Single(x.Species);
        Assert.Equal("", species.Name);
        Assert.Equal(new int[6], species.Base);
    }

    [Fact]
    public void Missing_order_gets_next_free_position()
    {
        const string json = """
            { "collection": "pokemon", "documents": {
              "a": { "name": "A", "order": 4 },
              "b": { "name": "B" }
            } }
            """;

        var all = PokemonJson.Deserialize(json)!;

        Assert.Equal(4, all["a"].Order);
        Assert.Equal(5, all["b"].Order);
    }

    [Fact]
    public void Preferences_round_trip()
    {
        var prefs = new UiPreferences { SelectedId = "a", Theme = "dark", Filter = "all", Language = "pt-BR" };

        var back = PokemonJson.DeserializePreferences(PokemonJson.SerializePreferences(prefs));

        Assert.Equal(prefs, back);
        Assert.Equal(new UiPreferences(), PokemonJson.DeserializePreferences("not json"));
    }

    [Theory]
    [InlineData("{}", null)]
    [InlineData("""{ "language": null }""", null)]
    [InlineData("""{ "language": "pt-BR" }""", "pt-BR")]
    [InlineData("""{ "language": "en", "theme": "dark" }""", "en")]
    public void Preferences_language_is_optional(string json, string? expected) =>
        Assert.Equal(expected, PokemonJson.DeserializePreferences(json).Language);

    [Theory]
    [InlineData("{}", "before")]
    [InlineData("""{ "guidePhase": "after" }""", "after")]
    [InlineData("""{ "guidePhase": "later" }""", "before")]
    public void Guide_phase_defaults_to_before_the_elite_four(string json, string expected) =>
        Assert.Equal(expected, PokemonJson.DeserializePreferences(json).GuidePhase);

    [Theory]
    [InlineData("{}", "fireRed")]
    [InlineData("""{ "gameVersion": "leafGreen" }""", "leafGreen")]
    [InlineData("""{ "gameVersion": "fireRed" }""", "fireRed")]
    [InlineData("""{ "gameVersion": "emerald" }""", "fireRed")]
    public void Game_version_defaults_to_fire_red(string json, string expected) =>
        Assert.Equal(expected, PokemonJson.DeserializePreferences(json).GameVersion);

    [Fact]
    public void Default_preferences_have_no_language_and_keep_it_that_way()
    {
        Assert.Null(new UiPreferences().Language);
        Assert.Null(PokemonJson.DeserializePreferences(PokemonJson.SerializePreferences(new UiPreferences { Theme = "dark" })).Language);
    }

    [Fact]
    public void Reads_and_writes_manual_adjustments_and_known_ivs()
    {
        const string json = """
            { "collection": "pokemon", "documents": { "x": {
              "name": "X",
              "events": [
                { "o": "Caterpie", "n": 8, "m": 1 },
                { "o": "", "n": 1, "m": 1, "set": [12, 0, 2, 26, 0, 4] },
                { "o": "", "n": 1, "m": 1, "set": [300, -5, 1] }
              ],
              "knownIvs": [null, null, null, null, null, 30]
            } } }
            """;

        var x = PokemonJson.Deserialize(json)!["x"];

        Assert.Null(x.Events[0].SetTotals);
        Assert.False(x.Events[0].IsManualAdjustment);
        Assert.Equal([12, 0, 2, 26, 0, 4], x.Events[1].SetTotals!);
        Assert.True(x.Events[1].IsManualAdjustment);
        Assert.Equal([255, 0, 1, 0, 0, 0], x.Events[2].SetTotals!);
        Assert.Equal([null, null, null, null, null, 30], x.KnownIvs);
    }

    [Fact]
    public void Missing_or_invalid_known_ivs_become_null()
    {
        const string json = """
            { "collection": "pokemon", "documents": {
              "a": { "name": "A" },
              "b": { "name": "B", "knownIvs": [32, -1, 5] }
            } }
            """;

        var all = PokemonJson.Deserialize(json)!;

        Assert.Equal(new int?[6], all["a"].KnownIvs);
        Assert.Equal([null, null, 5, null, null, null], all["b"].KnownIvs);
    }

    [Fact]
    public void Writes_set_only_on_adjustments_and_always_writes_known_ivs()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 1, 1));
        pokemon.Events.Add(new BattleEvent { Opponent = "", SetTotals = [1, 2, 3, 4, 5, 6] });
        pokemon.KnownIvs[1] = 20;

        var json = PokemonJson.Serialize(new Dictionary<string, TrainingPokemon> { ["sq"] = pokemon }, indented: false);
        var doc = JsonNode.Parse(json)!["documents"]!["sq"]!;

        Assert.Null(doc["events"]![0]!["set"]);
        Assert.Equal(6, doc["events"]![1]!["set"]!.AsArray().Count);
        Assert.Equal(6, doc["knownIvs"]!.AsArray().Count);
        Assert.Equal(20, doc["knownIvs"]![1]!.GetValue<int>());
        Assert.Equal(json, PokemonJson.Serialize(PokemonJson.Deserialize(json)!, indented: false));
    }

    [Fact]
    public void Updated_at_is_written_in_utc_iso_8601_and_read_back()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.UpdatedAt = new DateTimeOffset(2026, 10, 11, 18, 30, 15, TimeSpan.FromHours(-3)).AddTicks(1234);

        var json = PokemonJson.Serialize(new Dictionary<string, TrainingPokemon> { ["sq"] = pokemon }, indented: false);
        var text = JsonNode.Parse(json)!["documents"]!["sq"]!["updatedAt"]!.GetValue<string>();

        Assert.Equal("2026-10-11T21:30:15.0001234Z", text);
        Assert.Equal(pokemon.UpdatedAt, PokemonJson.Deserialize(json)!["sq"].UpdatedAt);
    }

    [Theory]
    [InlineData("""{ "collection": "pokemon", "documents": { "x": { "name": "X" } } }""")]
    [InlineData("""{ "collection": "pokemon", "documents": { "x": { "name": "X", "updatedAt": "ontem" } } }""")]
    [InlineData("""{ "collection": "pokemon", "documents": { "x": { "name": "X", "updatedAt": 5 } } }""")]
    public void Missing_or_invalid_updated_at_is_null(string json) =>
        Assert.Null(PokemonJson.Deserialize(json)!["x"].UpdatedAt);

    [Fact]
    public void Updated_at_is_not_written_when_unknown()
    {
        var json = PokemonJson.Serialize(new Dictionary<string, TrainingPokemon> { ["sq"] = Pokemons.Squirtle() }, indented: false);

        Assert.Null(JsonNode.Parse(json)!["documents"]!["sq"]!["updatedAt"]);
        Assert.Null(JsonNode.Parse(json)!["deleted"]);
    }

    [Fact]
    public void Deletions_are_read_and_written_with_the_documents()
    {
        var deleted = new Dictionary<string, DateTimeOffset> { ["gone"] = new(2026, 10, 10, 18, 2, 11, TimeSpan.Zero) };

        var json = PokemonJson.Serialize(new Dictionary<string, TrainingPokemon> { ["sq"] = Pokemons.Squirtle() }, indented: false, deleted);
        var snapshot = PokemonJson.DeserializeSnapshot(json)!;

        Assert.Equal("2026-10-10T18:02:11.0000000Z", JsonNode.Parse(json)!["deleted"]!["gone"]!.GetValue<string>());
        Assert.Equal(["sq"], snapshot.Documents.Keys);
        Assert.Equal(deleted, snapshot.Deleted);
    }

    [Fact]
    public void Invalid_deletions_are_ignored()
    {
        const string json = """
            { "collection": "pokemon", "documents": {}, "deleted": { "a": "2026-10-10T18:02:11Z", "b": "nunca", "c": 3 } }
            """;

        var snapshot = PokemonJson.DeserializeSnapshot(json)!;

        Assert.Equal(["a"], snapshot.Deleted.Keys);
    }
}