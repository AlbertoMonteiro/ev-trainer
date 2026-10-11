using System.Text.Json.Nodes;

using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class CaptureNotesJsonTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    private static CaptureNotes Read(string? json) => CaptureNotesJson.Deserialize(json, Dex, Data);

    [Fact]
    public void Round_trips()
    {
        var notes = new CaptureNotes();
        notes.Counted["Abra"] = [true, false, true, true, true, true];
        notes.Best["Abra"] = new CaptureInput("Abra", 8, "Timid", [21, 9, 10, 20, 15, 21]);

        var json = CaptureNotesJson.Serialize(notes);
        var back = Read(json);

        Assert.Equal([true, false, true, true, true, true], back.Counted["Abra"]);
        var best = back.Best["Abra"];
        Assert.Equal(("Abra", 8, "Timid"), (best.Species, best.Level, best.Nature));
        Assert.Equal([21, 9, 10, 20, 15, 21], best.Stats);
        Assert.Equal(json, CaptureNotesJson.Serialize(back));
    }

    [Fact]
    public void Writes_the_documented_property_names()
    {
        var notes = new CaptureNotes();
        notes.Counted["Abra"] = [true, false, true, true, true, true];
        notes.Best["Abra"] = new CaptureInput("Abra", 8, "Timid", [21, 9, 10, 20, 15, 21]);

        var root = JsonNode.Parse(CaptureNotesJson.Serialize(notes))!;

        Assert.Equal(6, root["counted"]!["Abra"]!.AsArray().Count);
        Assert.Equal(8, root["best"]!["Abra"]!["lv"]!.GetValue<int>());
        Assert.Equal("Timid", root["best"]!["Abra"]!["nature"]!.GetValue<string>());
        Assert.Equal(6, root["best"]!["Abra"]!["st"]!.AsArray().Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    public void Missing_or_invalid_document_is_empty(string? json)
    {
        var notes = Read(json);

        Assert.Empty(notes.Counted);
        Assert.Empty(notes.Best);
    }

    [Fact]
    public void Invalid_entries_are_dropped_and_names_are_canonical()
    {
        const string json = """
            {
              "counted": {
                "abra": [true, false, true, true, true, true],
                "Machop": [true, false],
                "Agumon": [true, true, true, true, true, true]
              },
              "best": {
                "squirtle": { "lv": 5, "nature": "Timid", "st": [20, 9, 12, 11, 12, 11] },
                "Abra": { "lv": 0, "nature": "Timid", "st": [21, 9, 10, 20, 15, 21] },
                "Machop": { "lv": 8, "nature": "Grumpy", "st": [21, 9, 10, 20, 15, 21] },
                "Geodude": { "lv": 8, "nature": "Hardy", "st": [21, 9, 10, 20, 15] },
                "Onix": { "lv": 8, "nature": "Hardy", "st": [21, 9, 10, 20, 15, 1000] },
                "Agumon": { "lv": 8, "nature": "Hardy", "st": [21, 9, 10, 20, 15, 21] }
              }
            }
            """;

        var notes = Read(json);

        Assert.Equal(["Abra"], notes.Counted.Keys);
        Assert.Equal(["Squirtle"], notes.Best.Keys);
        Assert.Equal("Squirtle", notes.Best["Squirtle"].Species);
    }
}