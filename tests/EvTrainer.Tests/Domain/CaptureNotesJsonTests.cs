using System.Text.Json.Nodes;

using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class CaptureNotesJsonTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    private const StatImportance I = StatImportance.Ignored;
    private const StatImportance C = StatImportance.Counts;
    private const StatImportance M = StatImportance.Main;

    private static CaptureNotes Read(string? json) => CaptureNotesJson.Deserialize(json, Dex, Data);

    [Fact]
    public void Round_trips()
    {
        var notes = new CaptureNotes();
        notes.Importance["Abra"] = [C, I, C, M, C, M];
        notes.Best["Abra"] = new CaptureInput("Abra", 8, "Timid", [21, 9, 10, 20, 15, 21]);

        var json = CaptureNotesJson.Serialize(notes);
        var back = Read(json);

        Assert.Equal([C, I, C, M, C, M], back.Importance["Abra"]);
        var best = back.Best["Abra"];
        Assert.Equal(("Abra", 8, "Timid"), (best.Species, best.Level, best.Nature));
        Assert.Equal([21, 9, 10, 20, 15, 21], best.Stats);
        Assert.Equal(json, CaptureNotesJson.Serialize(back));
    }

    [Fact]
    public void Writes_the_documented_property_names()
    {
        var notes = new CaptureNotes();
        notes.Importance["Abra"] = [C, I, C, M, C, C];
        notes.Best["Abra"] = new CaptureInput("Abra", 8, "Timid", [21, 9, 10, 20, 15, 21]);

        var root = JsonNode.Parse(CaptureNotesJson.Serialize(notes))!;

        Assert.Equal(
            ["counts", "ignored", "counts", "main", "counts", "counts"],
            root["importance"]!["Abra"]!.AsArray().Select(v => v!.GetValue<string>()));
        Assert.Null(root["counted"]);
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

        Assert.Empty(notes.Importance);
        Assert.Empty(notes.Best);
    }

    [Fact]
    public void Invalid_entries_are_dropped_and_names_are_canonical()
    {
        const string json = """
            {
              "importance": {
                "abra": ["counts", "ignored", "counts", "main", "counts", "counts"],
                "Machop": ["main", "counts"],
                "Geodude": ["main", "counts", "counts", "counts", "counts", "huge"],
                "Agumon": ["counts", "counts", "counts", "counts", "counts", "counts"]
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

        Assert.Equal(["Abra"], notes.Importance.Keys);
        Assert.Equal(["Squirtle"], notes.Best.Keys);
        Assert.Equal("Squirtle", notes.Best["Squirtle"].Species);
    }

    [Fact]
    public void Legacy_counted_is_converted_and_never_written_back()
    {
        // Machop default: [C, M, C, I, C, C]. Legacy true keeps the default (ATK stays main, but the
        // default-ignored SPA becomes "counts"); false becomes "ignored".
        const string json = """
            {
              "counted": {
                "Machop": [true, true, false, true, true, false],
                "Abra": [true, false, true, true, true, true]
              },
              "importance": { "Abra": ["main", "main", "main", "main", "main", "main"] }
            }
            """;

        var notes = Read(json);

        Assert.Equal([C, M, I, C, C, I], notes.Importance["Machop"]);
        Assert.Equal([M, M, M, M, M, M], notes.Importance["Abra"]);
        Assert.Null(JsonNode.Parse(CaptureNotesJson.Serialize(notes))!["counted"]);
    }
}