using System.Text.Json;
using System.Text.Json.Serialization;

using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain.Json;

/// <summary>
/// Reads and writes the capture notes document (<c>evtrainer:v1:capture</c>, data-model.md). Reading is
/// tolerant: invalid entries are dropped and species names become the Pokédex's canonical names.
/// </summary>
public static class CaptureNotesJson
{
    public static string Serialize(CaptureNotes notes) => JsonSerializer.Serialize(
        new CaptureNotesDto
        {
            Counted = notes.Counted.ToDictionary(kv => kv.Key, kv => (bool[]?)[.. kv.Value]),
            Best = notes.Best.ToDictionary(kv => kv.Key, kv => (BestCaptureDto?)new BestCaptureDto
            {
                Level = kv.Value.Level,
                Nature = kv.Value.Nature,
                Stats = [.. kv.Value.Stats],
            }),
        },
        EvTrainerJsonContext.Default.CaptureNotesDto);

    public static CaptureNotes Deserialize(string? json, Pokedex dex, ReferenceData data)
    {
        var notes = new CaptureNotes();
        if (string.IsNullOrWhiteSpace(json) || Parse(json) is not { } dto)
        {
            return notes;
        }

        foreach (var (name, counted) in dto.Counted ?? [])
        {
            if (dex.Find(name) is { } species && counted is { Length: StatVector.Count })
            {
                notes.Counted[species.Name] = [.. counted];
            }
        }

        foreach (var (name, best) in dto.Best ?? [])
        {
            if (dex.Find(name) is { } species
                && best is { Level: { } level, Nature: { } nature, Stats: { } stats }
                && new CaptureInput(species.Name, level, nature, [.. stats]) is var input
                && input.IsValid(dex, data))
            {
                notes.Best[species.Name] = input;
            }
        }

        return notes;
    }

    private static CaptureNotesDto? Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize(json, EvTrainerJsonContext.Default.CaptureNotesDto);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal sealed class CaptureNotesDto
{
    [JsonPropertyName("counted")]
    public Dictionary<string, bool[]?>? Counted { get; set; }

    [JsonPropertyName("best")]
    public Dictionary<string, BestCaptureDto?>? Best { get; set; }
}

internal sealed class BestCaptureDto
{
    [JsonPropertyName("lv")]
    public int? Level { get; set; }

    [JsonPropertyName("nature")]
    public string? Nature { get; set; }

    [JsonPropertyName("st")]
    public int[]? Stats { get; set; }
}