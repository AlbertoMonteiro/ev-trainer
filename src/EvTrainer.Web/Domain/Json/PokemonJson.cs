using System.Globalization;
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

    /// <param name="deleted">Deletion records (sync, spec 008); written only when there are any.</param>
    public static string Serialize(
        IReadOnlyDictionary<string, TrainingPokemon> all,
        bool indented,
        IReadOnlyDictionary<string, DateTimeOffset>? deleted = null)
    {
        var dto = new BackupDto
        {
            Collection = Collection,
            Documents = all.ToDictionary(kv => kv.Key, kv => (PokemonDto?)ToDto(kv.Value)),
            Deleted = deleted is { Count: > 0 } ? deleted.ToDictionary(kv => kv.Key, kv => ToElement(kv.Value)) : null,
        };
        var context = indented ? EvTrainerJsonContext.Indented : EvTrainerJsonContext.Default;
        return JsonSerializer.Serialize(dto, context.BackupDto);
    }

    /// <summary>Parses a document. Returns null when the JSON is invalid or not a pokemon collection.</summary>
    public static Dictionary<string, TrainingPokemon>? Deserialize(string json) =>
        TryParse(json, out var dto) && dto.Collection == Collection && dto.Documents is not null
            ? FromDocuments(dto.Documents)
            : null;

    /// <summary>Documents and deletion records of a data document; null when it is not a pokemon collection.</summary>
    public static SyncSnapshot? DeserializeSnapshot(string json)
    {
        if (!TryParse(json, out var dto) || dto.Collection != Collection || dto.Documents is null)
        {
            return null;
        }

        var deleted = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        foreach (var (id, value) in dto.Deleted ?? [])
        {
            if (ParseTimestamp(value) is { } at)
            {
                deleted[id] = at;
            }
        }

        return new SyncSnapshot(FromDocuments(dto.Documents), deleted);
    }

    /// <summary>ISO 8601 in UTC with full precision, so a value read back is identical to the one written.</summary>
    internal static string FormatTimestamp(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);

    private static JsonElement ToElement(DateTimeOffset value) =>
        JsonSerializer.SerializeToElement(FormatTimestamp(value), EvTrainerJsonContext.Default.String);

    private static DateTimeOffset? ParseTimestamp(JsonElement? value) =>
        value is { ValueKind: JsonValueKind.String } text
        && DateTimeOffset.TryParse(text.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var at)
            ? at
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
            var preferences = JsonSerializer.Deserialize(json, EvTrainerJsonContext.Default.UiPreferences) ?? new UiPreferences();
            return preferences with
            {
                GuidePhase = preferences.GuidePhase is "before" or "after" ? preferences.GuidePhase : "before",
                GameVersion = GameVersions.Key(GameVersions.Parse(preferences.GameVersion)),
            };
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
            Nature = dto.Nature ?? "",
            Species = species,
            Targets = (dto.Targets ?? []).ToSix(),
            Events = [.. (dto.Events ?? []).Where(e => e is not null).Select(e => new BattleEvent
            {
                Opponent = e!.Opponent ?? "",
                Count = e.Count is { } n && n >= 1 ? n : 1,
                Multiplier = e.Multiplier is 1 or 2 or 4 ? e.Multiplier.Value : 1,
                SetTotals = e.SetTotals is { } set ? [.. set.ToSix().Select(v => Math.Clamp(v, 0, StatVector.MaxEvPerStat))] : null,
                Shared = e.Shared == true,
            })],
            Pokerus = dto.Pokerus,
            MachoBrace = dto.Macho,
            ExpShare = dto.ExpShare == true,
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

        // "name" was a free nickname before; it now holds the current species.
        pokemon.CurrentSpecies = CurrentForm.Resolve(dto.Name, pokemon.Species, pokemon.IvLog);
        pokemon.UpdatedAt = ParseTimestamp(dto.UpdatedAt);
        return pokemon;
    }

    private static PokemonDto ToDto(TrainingPokemon p) => new()
    {
        Name = p.CurrentSpecies,
        Nature = p.Nature,
        Species = [.. p.Species.Select(s => new SpeciesDto { Name = s.Name, Base = [.. s.Base] })],
        Targets = [.. p.Targets],
        Events = [.. p.Events.Select(e => new EventDto
        {
            Opponent = e.Opponent,
            Count = e.Count,
            Multiplier = e.Multiplier,
            SetTotals = e.SetTotals is null ? null : [.. e.SetTotals],
            Shared = e.Shared ? true : null,
        })],
        IvLog = [.. p.IvLog.Select(l => new LevelDto { Level = l.Level, Species = l.Species, Stats = [.. l.Stats], Evs = [.. l.Evs] })],
        Pokerus = p.Pokerus,
        Macho = p.MachoBrace,
        ExpShare = p.ExpShare ? true : null,
        Order = p.Order,
        KnownIvs = [.. p.KnownIvs],
        UpdatedAt = p.UpdatedAt is { } at ? ToElement(at) : null,
    };
}

internal sealed class BackupDto
{
    [JsonPropertyName("collection")]
    public string? Collection { get; set; }

    [JsonPropertyName("documents")]
    public Dictionary<string, PokemonDto?>? Documents { get; set; }

    /// <summary>Deletion records (id → ISO date), read tolerantly.</summary>
    [JsonPropertyName("deleted")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? Deleted { get; set; }
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

    /// <summary>Exp. Share holder (spec 012); written only when true.</summary>
    [JsonPropertyName("expShare")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ExpShare { get; set; }

    [JsonPropertyName("order")]
    public int? Order { get; set; }

    [JsonPropertyName("knownIvs")]
    public int?[]? KnownIvs { get; set; }

    /// <summary>ISO date, read tolerantly (anything else becomes null).</summary>
    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? UpdatedAt { get; set; }
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

    /// <summary>Received through the Exp. Share (spec 012); written only when true.</summary>
    [JsonPropertyName("x")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Shared { get; set; }
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
[JsonSerializable(typeof(CaptureNotesDto))]
[JsonSerializable(typeof(string))]
internal sealed partial class EvTrainerJsonContext : JsonSerializerContext
{
    public static EvTrainerJsonContext Indented { get; } = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
}