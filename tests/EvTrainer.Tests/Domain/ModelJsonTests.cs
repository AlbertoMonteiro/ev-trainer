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
            ["events", "ivLog", "macho", "name", "nature", "order", "pokerus", "species", "targets"],
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
}