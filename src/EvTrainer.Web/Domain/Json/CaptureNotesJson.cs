using System.Text.Json;
using System.Text.Json.Serialization;

using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain.Json;

/// <summary>
/// Reads and writes the capture notes document (<c>evtrainer:v1:capture</c>, data-model.md). Reading is
/// tolerant: invalid entries are dropped and species names become the Pokédex's canonical names. The legacy
/// <c>counted</c> booleans are converted to importance (research R10) and never written again.
/// </summary>
public static class CaptureNotesJson
{
    private static readonly Dictionary<string, StatImportance> Names = new(StringComparer.Ordinal)
    {
        ["main"] = StatImportance.Main,
        ["counts"] = StatImportance.Counts,
        ["ignored"] = StatImportance.Ignored,
    };

    public static string Serialize(CaptureNotes notes) => JsonSerializer.Serialize(
        new CaptureNotesDto
        {
            Importance = notes.Importance.ToDictionary(kv => kv.Key, kv => (string?[]?)[.. kv.Value.Select(NameOf)]),
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

        foreach (var (name, values) in dto.Importance ?? [])
        {
            if (dex.Find(name) is { } species
                && values is { Length: StatVector.Count }
                && values.All(v => v is not null && Names.ContainsKey(v)))
            {
                notes.Importance[species.Name] = [.. values.Select(v => Names[v!])];
            }
        }

        foreach (var (name, counted) in dto.Counted ?? [])
        {
            if (dex.Find(name) is { } species && counted is { Length: StatVector.Count } && !notes.Importance.ContainsKey(species.Name))
            {
                var defaults = Capture.DefaultImportance(species.Base);
                notes.Importance[species.Name] =
                [
                    .. counted.Select((counts, i) => !counts ? StatImportance.Ignored
                        : defaults[i] == StatImportance.Ignored ? StatImportance.Counts
                        : defaults[i]),
                ];
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

    private static string NameOf(StatImportance importance) => Names.First(kv => kv.Value == importance).Key;

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
    [JsonPropertyName("importance")]
    public Dictionary<string, string?[]?>? Importance { get; set; }

    /// <summary>Legacy (before amendment R10): read only.</summary>
    [JsonPropertyName("counted")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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