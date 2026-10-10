using System.Text.Json;
using System.Text.Json.Serialization;

using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.Domain.Json;

/// <summary>
/// Reads and writes the backup/storage document <c>{ "collection": "pokemon", "documents": { id: Pokemon } }</c>
/// with the HANDOFF §3.1 property names. Reading is tolerant (contracts/backup-format.md).
/// </summary>
public static class PokemonJson
{
    public const string Collection = "pokemon";

    public static string Serialize(IReadOnlyDictionary<string, TrainingPokemon> all, bool indented)
    {
        var dto = new BackupDto
        {
            Collection = Collection,
            Documents = all.ToDictionary(kv => kv.Key, kv => (PokemonDto?)ToDto(kv.Value)),
        };
        var context = indented ? EvTrainerJsonContext.Indented : EvTrainerJsonContext.Default;
        return JsonSerializer.Serialize(dto, context.BackupDto);
    }

    /// <summary>Parses a document. Returns null when the JSON is invalid or not a pokemon collection.</summary>
    public static Dictionary<string, TrainingPokemon>? Deserialize(string json) =>
        TryParse(json, out var dto) && dto.Collection == Collection && dto.Documents is not null
            ? FromDocuments(dto.Documents)
            : null;

    internal static bool TryParse(string json, out BackupDto dto)
    {
        try
        {
            dto = JsonSerializer.Deserialize(json, EvTrainerJsonContext.Default.BackupDto) ?? new BackupDto();
            return true;
        }
        catch (JsonException)
        {
            dto = new BackupDto();
            return false;
        }
    }

    internal static Dictionary<string, TrainingPokemon> FromDocuments(Dictionary<string, PokemonDto?> documents)
    {
        var result = new Dictionary<string, TrainingPokemon>(StringComparer.Ordinal);
        var nextOrder = documents.Values.Max(d => d?.Order) ?? 0;
        foreach (var (id, dto) in documents)
        {
            var pokemon = FromDto(dto ?? new PokemonDto());
            pokemon.Order = dto?.Order ?? ++nextOrder;
            result[id] = pokemon;
        }

        return result;
    }

    public static string SerializePreferences(UiPreferences preferences) =>
        JsonSerializer.Serialize(preferences, EvTrainerJsonContext.Default.UiPreferences);

    public static UiPreferences DeserializePreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new UiPreferences();
        }

        try
        {
            return JsonSerializer.Deserialize(json, EvTrainerJsonContext.Default.UiPreferences) ?? new UiPreferences();
        }
        catch (JsonException)
        {
            return new UiPreferences();
        }
    }

    private static TrainingPokemon FromDto(PokemonDto dto)
    {
        List<SpeciesEntry> species = [.. (dto.Species ?? []).Select(s => new SpeciesEntry
        {
            Name = s?.Name ?? "",
            Base = (s?.Base ?? []).ToSix(),
        })];
        if (species.Count == 0)
        {
            species.Add(new SpeciesEntry());
        }

        var pokemon = new TrainingPokemon
        {
            Name = dto.Name ?? "",
            Nature = dto.Nature ?? "",
            Species = species,
            Targets = (dto.Targets ?? []).ToSix(),
            Events = [.. (dto.Events ?? []).Where(e => e is not null).Select(e => new BattleEvent
            {
                Opponent = e!.Opponent ?? "",
                Count = e.Count is { } n && n >= 1 ? n : 1,
                Multiplier = e.Multiplier is 1 or 2 or 4 ? e.Multiplier.Value : 1,
                SetTotals = e.SetTotals is { } set ? [.. set.ToSix().Select(v => Math.Clamp(v, 0, StatVector.MaxEvPerStat))] : null,
            })],
            Pokerus = dto.Pokerus,
            MachoBrace = dto.Macho,
            KnownIvs = [.. Enumerable.Range(0, StatVector.Count).Select(i =>
                dto.KnownIvs is { } known && i < known.Length && known[i] is >= 0 and <= StatVector.MaxIv ? known[i] : null)],
        };

        foreach (var level in (dto.IvLog ?? []).Where(l => l is not null).OrderBy(l => l!.Level))
        {
            pokemon.IvLog.Add(new LevelEntry
            {
                Level = level!.Level,
                Species = level.Species ?? "",
                Stats = (level.Stats ?? []).ToSix(),
                Evs = (level.Evs ?? []).ToSix(),
            });
        }

        return pokemon;
    }

    private static PokemonDto ToDto(TrainingPokemon p) => new()
    {
        Name = p.Name,
        Nature = p.Nature,
        Species = [.. p.Species.Select(s => new SpeciesDto { Name = s.Name, Base = [.. s.Base] })],
        Targets = [.. p.Targets],
        Events = [.. p.Events.Select(e => new EventDto
        {
            Opponent = e.Opponent,
            Count = e.Count,
            Multiplier = e.Multiplier,
            SetTotals = e.SetTotals is null ? null : [.. e.SetTotals],
        })],
        IvLog = [.. p.IvLog.Select(l => new LevelDto { Level = l.Level, Species = l.Species, Stats = [.. l.Stats], Evs = [.. l.Evs] })],
        Pokerus = p.Pokerus,
        Macho = p.MachoBrace,
        Order = p.Order,
        KnownIvs = [.. p.KnownIvs],
    };
}

internal sealed class BackupDto
{
    [JsonPropertyName("collection")]
    public string? Collection { get; set; }

    [JsonPropertyName("documents")]
    public Dictionary<string, PokemonDto?>? Documents { get; set; }
}

internal sealed class PokemonDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("nature")]
    public string? Nature { get; set; }

    [JsonPropertyName("species")]
    public List<SpeciesDto?>? Species { get; set; }

    [JsonPropertyName("targets")]
    public int[]? Targets { get; set; }

    [JsonPropertyName("events")]
    public List<EventDto?>? Events { get; set; }

    [JsonPropertyName("ivLog")]
    public List<LevelDto?>? IvLog { get; set; }

    [JsonPropertyName("pokerus")]
    public bool Pokerus { get; set; }

    [JsonPropertyName("macho")]
    public bool Macho { get; set; }

    [JsonPropertyName("order")]
    public int? Order { get; set; }

    [JsonPropertyName("knownIvs")]
    public int?[]? KnownIvs { get; set; }
}

internal sealed class SpeciesDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("base")]
    public int[]? Base { get; set; }
}

internal sealed class EventDto
{
    [JsonPropertyName("o")]
    public string? Opponent { get; set; }

    [JsonPropertyName("n")]
    public int? Count { get; set; }

    [JsonPropertyName("m")]
    public int? Multiplier { get; set; }

    [JsonPropertyName("set")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? SetTotals { get; set; }
}

internal sealed class LevelDto
{
    [JsonPropertyName("lv")]
    public int Level { get; set; }

    [JsonPropertyName("sp")]
    public string? Species { get; set; }

    [JsonPropertyName("st")]
    public int[]? Stats { get; set; }

    [JsonPropertyName("ev")]
    public int[]? Evs { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BackupDto))]
[JsonSerializable(typeof(UiPreferences))]
internal sealed partial class EvTrainerJsonContext : JsonSerializerContext
{
    public static EvTrainerJsonContext Indented { get; } = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
}