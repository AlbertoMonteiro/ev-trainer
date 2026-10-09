using System.Text.Json;

using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.Domain.Reference;

/// <summary>Opponent (Kanto Pokémon or vitamin) with its EV yield [HP, Ataque, Defesa, SpAtk, SpDef, Speed].</summary>
public sealed record Opponent(int? Dex, string Name, int[] EvYield, string WildLocation, bool IsVitamin);

/// <summary>Trainer location and the opponents its trainers use, in file order.</summary>
public sealed record TrainerLocation(string Name, IReadOnlyList<string> Opponents);

/// <summary>Nature; <see cref="Increases"/>/<see cref="Decreases"/> are stat indexes, or null when neutral.</summary>
public sealed record Nature(string Name, int? Increases, int? Decreases)
{
    public bool IsNeutral => Increases is null;

    /// <summary>Integer multiplier: 11 for the raised stat, 9 for the lowered one, 10 otherwise (always 10 for HP).</summary>
    public int Multiplier(int statIndex) =>
        statIndex == StatVector.Hp ? 10
        : statIndex == Increases ? 11
        : statIndex == Decreases ? 9
        : 10;
}

/// <summary>Static game data embedded from reference-data.json.</summary>
public sealed class ReferenceData
{
    private const string ResourceName = "EvTrainer.reference-data.json";

    private readonly Dictionary<string, Opponent> _byName;
    private readonly Dictionary<string, string> _aliases;
    private readonly Dictionary<string, Nature> _natures;

    private ReferenceData(
        IReadOnlyList<string> statNames,
        IReadOnlyList<string> statsShort,
        IReadOnlyList<Opponent> opponents,
        Dictionary<string, string> aliases,
        IReadOnlyList<TrainerLocation> trainers,
        IReadOnlyList<Nature> natures)
    {
        StatNames = statNames;
        StatsShort = statsShort;
        Opponents = opponents;
        TrainersByLocation = trainers;
        Natures = natures;
        _aliases = aliases;
        _byName = opponents.ToDictionary(o => o.Name, StringComparer.Ordinal);
        _natures = natures.ToDictionary(n => n.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<string> StatNames { get; }

    public IReadOnlyList<string> StatsShort { get; }

    public IReadOnlyList<Opponent> Opponents { get; }

    public IReadOnlyList<TrainerLocation> TrainersByLocation { get; }

    public IReadOnlyList<Nature> Natures { get; }

    public static ReferenceData LoadEmbedded()
    {
        using var stream = typeof(ReferenceData).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
        using var document = JsonDocument.Parse(stream);
        return Parse(document.RootElement);
    }

    /// <summary>Applies aliases (old spreadsheet names); unknown names are returned unchanged.</summary>
    public string Canon(string name) => _aliases.GetValueOrDefault(name, name);

    public Opponent? Lookup(string name) => _byName.GetValueOrDefault(Canon(name));

    /// <summary>Nature by name; "" or unknown returns null (neutral).</summary>
    public Nature? FindNature(string name) => _natures.GetValueOrDefault(name);

    private static ReferenceData Parse(JsonElement root)
    {
        var statNames = root.GetProperty("stats").EnumerateArray().Select(s => s.GetString()!).ToList();
        var statsShort = root.GetProperty("statsShort").EnumerateArray().Select(s => s.GetString()!).ToList();

        var opponents = root.GetProperty("opponents").EnumerateArray()
            .Select(o => new Opponent(
                o.GetProperty("dex").ValueKind == JsonValueKind.Number ? o.GetProperty("dex").GetInt32() : null,
                o.GetProperty("name").GetString()!,
                [.. o.GetProperty("evYield").EnumerateArray().Select(v => v.GetInt32())],
                o.TryGetProperty("wildLocation", out var wild) ? wild.GetString() ?? "" : "",
                o.GetProperty("isVitamin").GetBoolean()))
            .ToList();

        var aliases = root.GetProperty("aliases").EnumerateObject()
            .ToDictionary(a => a.Name, a => a.Value.GetString()!, StringComparer.Ordinal);

        var trainers = root.GetProperty("trainersByLocation").EnumerateObject()
            .Select(l => new TrainerLocation(l.Name, [.. l.Value.EnumerateArray().Select(n => n.GetString()!)]))
            .ToList();

        int? StatIndex(JsonElement value) =>
            value.ValueKind == JsonValueKind.String ? statNames.IndexOf(value.GetString()!) : null;

        var natures = root.GetProperty("natures").EnumerateArray()
            .Select(n => new Nature(
                n.GetProperty("name").GetString()!,
                StatIndex(n.GetProperty("increases")),
                StatIndex(n.GetProperty("decreases"))))
            .ToList();

        return new ReferenceData(statNames, statsShort, opponents, aliases, trainers, natures);
    }
}