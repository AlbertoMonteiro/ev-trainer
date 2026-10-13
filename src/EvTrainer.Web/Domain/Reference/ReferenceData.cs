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
    private const string LocationsResourceName = "EvTrainer.locations.json";

    private readonly Dictionary<string, Opponent> _byName;
    private readonly Dictionary<string, string> _aliases;
    private readonly Dictionary<string, Nature> _natures;

    private ReferenceData(
        IReadOnlyList<string> statNames,
        IReadOnlyList<string> statsShort,
        IReadOnlyList<Opponent> opponents,
        Dictionary<string, string> aliases,
        IReadOnlyList<TrainerLocation> trainers,
        IReadOnlyDictionary<GameVersion, IReadOnlyDictionary<string, IReadOnlyList<string>>> wildPlaces,
        IReadOnlyList<Nature> natures)
    {
        _wildPlaces = wildPlaces;
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

    private readonly IReadOnlyDictionary<GameVersion, IReadOnlyDictionary<string, IReadOnlyList<string>>> _wildPlaces;

    public IReadOnlyList<Nature> Natures { get; }

    /// <summary>
    /// Curated Kanto data from reference-data.json plus every other Gen III species (#152–386) from the
    /// embedded Pokédex, so any Pokémon met in FireRed can be counted. Vitamins stay at the end. Every place of the
    /// game (routes, cities, gyms, dungeons, Sevii Islands) comes from locations.json, generated from the FireRed
    /// decompilation: its trainers join the curated ones of the same name, its wild species (per version) go to <see cref="WildPlaces"/>.
    /// </summary>
    public static ReferenceData LoadEmbedded()
    {
        using var document = JsonDocument.Parse(Resource(ResourceName));
        using var locations = JsonDocument.Parse(Resource(LocationsResourceName));
        return Parse(document.RootElement, locations.RootElement, Pokedex.LoadEmbedded());
    }

    private static Stream Resource(string name) =>
        typeof(ReferenceData).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");

    /// <summary>Places (Portuguese name) where each species is found in the wild in the version, from locations.json.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> WildPlaces(GameVersion version) => _wildPlaces[version];

    /// <summary>Applies aliases (old spreadsheet names); unknown names are returned unchanged.</summary>
    public string Canon(string name) => _aliases.GetValueOrDefault(name, name);

    public Opponent? Lookup(string name) => _byName.GetValueOrDefault(Canon(name));

    /// <summary>Nature by name; "" or unknown returns null (neutral).</summary>
    public Nature? FindNature(string name) => _natures.GetValueOrDefault(name);

    private static ReferenceData Parse(JsonElement root, JsonElement locations, Pokedex pokedex)
    {
        var statNames = root.GetProperty("stats").EnumerateArray().Select(s => s.GetString()!).ToList();
        var statsShort = root.GetProperty("statsShort").EnumerateArray().Select(s => s.GetString()!).ToList();

        var curated = root.GetProperty("opponents").EnumerateArray()
            .Select(o => new Opponent(
                o.GetProperty("dex").ValueKind == JsonValueKind.Number ? o.GetProperty("dex").GetInt32() : null,
                o.GetProperty("name").GetString()!,
                [.. o.GetProperty("evYield").EnumerateArray().Select(v => v.GetInt32())],
                o.TryGetProperty("wildLocation", out var wild) ? wild.GetString() ?? "" : "",
                o.GetProperty("isVitamin").GetBoolean()))
            .ToList();
        var lastCurated = curated.Where(o => !o.IsVitamin).Max(o => o.Dex ?? 0);
        List<Opponent> opponents =
        [
            .. curated.Where(o => !o.IsVitamin),
            .. pokedex.Species
                .Where(s => s.Dex > lastCurated)
                .Select(s => new Opponent(s.Dex, s.Name, [.. s.EvYield], WildLocation: "", IsVitamin: false)),
            .. curated.Where(o => o.IsVitamin),
        ];

        var aliases = root.GetProperty("aliases").EnumerateObject()
            .ToDictionary(a => a.Name, a => a.Value.GetString()!, StringComparer.Ordinal);

        var trainers = root.GetProperty("trainersByLocation").EnumerateObject()
            .Select(l => new TrainerLocation(l.Name, [.. l.Value.EnumerateArray().Select(n => n.GetString()!)]))
            .ToList();

        var wildPlaces = new Dictionary<GameVersion, Dictionary<string, List<string>>>
        {
            [GameVersion.FireRed] = new(StringComparer.Ordinal),
            [GameVersion.LeafGreen] = new(StringComparer.Ordinal),
        };
        foreach (var place in locations.GetProperty("places").EnumerateArray())
        {
            var name = place.GetProperty("pt").GetString()!;
            List<string> team = [.. place.GetProperty("trainers").EnumerateArray().Select(n => n.GetString()!)];
            var index = trainers.FindIndex(t => t.Name == name);
            if (index >= 0)
            {
                trainers[index] = trainers[index] with { Opponents = [.. trainers[index].Opponents.Union(team)] };
            }
            else if (team.Count > 0)
            {
                trainers.Add(new TrainerLocation(name, team));
            }

            foreach (var (version, bySpecies) in wildPlaces)
            {
                foreach (var species in place.GetProperty("wild").GetProperty(GameVersions.Key(version)).EnumerateArray().Select(n => n.GetString()!))
                {
                    (bySpecies.TryGetValue(species, out var list) ? list : bySpecies[species] = []).Add(name);
                }
            }
        }

        int? StatIndex(JsonElement value) =>
            value.ValueKind == JsonValueKind.String ? statNames.IndexOf(value.GetString()!) : null;

        var natures = root.GetProperty("natures").EnumerateArray()
            .Select(n => new Nature(
                n.GetProperty("name").GetString()!,
                StatIndex(n.GetProperty("increases")),
                StatIndex(n.GetProperty("decreases"))))
            .ToList();

        return new ReferenceData(
            statNames,
            statsShort,
            opponents,
            aliases,
            trainers,
            wildPlaces.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyDictionary<string, IReadOnlyList<string>>)kv.Value.ToDictionary(
                    s => s.Key, s => (IReadOnlyList<string>)s.Value, StringComparer.Ordinal)),
            natures);
    }
}