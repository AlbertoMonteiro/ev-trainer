// Generates locations.json (every place of FireRed/LeafGreen with the species its trainers use and the wild species found
// there in each version)
// from the FireRed decompilation (pret/pokefirered). Run from the repository root:  dotnet run scripts/BuildLocations.cs
//
// Places are the region map sections (data/maps/*/map.json "region_map_section", names in
// src/data/region_map/region_map_sections.json); gyms are their own place ("Pewter Gym (Brock)").
// Trainers: script labels are named after their map (Route20_EventScript_Barry in data/scripts/trainers.inc or in the
// map's scripts.inc); every trainerbattle in them is a trainer of that map, plus its VS Seeker rematch parties
// (src/vs_seeker.c); trainers are the same in both versions. Wild: src/data/wild_encounters.json, the _FireRed and
// _LeafGreen tables (land, water, rock smash, fishing), kept per version.
#:property TreatWarningsAsErrors=false
#:property PublishAot=false

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

const string Commit = "037335f4c725d7c9aecdac87066f2002b4bd7e14";
const string BaseUrl = $"https://raw.githubusercontent.com/pret/pokefirered/{Commit}/";

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
async Task<string?> Get(string path)
{
    for (var attempt = 0; ; attempt++)
    {
        using var response = await http.GetAsync(BaseUrl + path);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        if (attempt == 3)
        {
            response.EnsureSuccessStatusCode();
        }

        await Task.Delay(1000 * (attempt + 1));
    }
}

// Species names as the app knows them (Pokédex Gen III).
var speciesByKey = new Dictionary<string, string>();
foreach (var s in JsonNode.Parse(File.ReadAllText("pokedex-gen3.json"))!["species"]!.AsArray())
{
    speciesByKey.TryAdd(Key((string)s!["name"]!), (string)s!["name"]!);
}

speciesByKey["NIDORANF"] = "Nidoran♀";
speciesByKey["NIDORANM"] = "Nidoran♂";

// Trainers (class, name, party) and parties (species).
var trainersSource = (await Get("src/data/trainers.h"))!;
var partiesSource = (await Get("src/data/trainer_parties.h"))!;
var trainers = new Dictionary<string, (string Class, string Name, string Party)>();
foreach (Match m in Regex.Matches(trainersSource, @"\[(TRAINER_\w+)\] = \{(.*?)\n    \},", RegexOptions.Singleline))
{
    var body = m.Groups[2].Value;
    var cls = Regex.Match(body, @"trainerClass = TRAINER_CLASS_(\w+)");
    var party = Regex.Match(body, @"party = \w+\((sParty_\w+)\)");
    if (cls.Success && party.Success)
    {
        trainers[m.Groups[1].Value] = (cls.Groups[1].Value, Regex.Match(body, @"trainerName = _\(""([^""]*)""\)").Groups[1].Value, party.Groups[1].Value);
    }
}

var parties = new Dictionary<string, List<string>>();
foreach (Match m in Regex.Matches(partiesSource, @"(sParty_\w+)\[\] = \{([^;]*)\};"))
{
    parties[m.Groups[1].Value] = [.. Regex.Matches(m.Groups[2].Value, @"\.species = SPECIES_(\w+)").Select(p => Species(p.Groups[1].Value))];
}

// VS Seeker rematch parties count for the place of the original trainer.
var vsSeeker = (await Get("src/vs_seeker.c"))!;
var table = vsSeeker[vsSeeker.IndexOf("sRematches[] = {", StringComparison.Ordinal)..];
table = table[..table.IndexOf("};", StringComparison.Ordinal)];
var rematches = new Dictionary<string, List<string>>();
foreach (Match m in Regex.Matches(table, @"\{\s*\{([^{}]*)\},\s*MAP\((\w+)\)\s*\}"))
{
    var ids = m.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    rematches[ids[0]] = [.. ids.Skip(1).Where(id => id != "SKIP" && id != ids[0])];
}

// Script labels and the trainers they battle, from the shared trainer scripts and every map's scripts.
var mapGroups = JsonNode.Parse((await Get("data/maps/map_groups.json"))!)!;
List<string> maps = [.. mapGroups["group_order"]!.AsArray().SelectMany(g => mapGroups[(string)g!]!.AsArray().Select(m => (string)m!))];
var scriptSources = new List<string> { (await Get("data/scripts/trainers.inc"))! };
var mapInfo = new Dictionary<string, (string Id, string Section)>();
using (var gate = new SemaphoreSlim(12))
{
    var loads = maps.Select(async map =>
    {
        await gate.WaitAsync();
        try
        {
            var json = JsonNode.Parse((await Get($"data/maps/{map}/map.json"))!)!;
            var scripts = await Get($"data/maps/{map}/scripts.inc");
            lock (mapInfo)
            {
                mapInfo[map] = ((string)json["id"]!, (string)json["region_map_section"]!);
                if (scripts is not null)
                {
                    scriptSources.Add(scripts);
                }
            }
        }
        finally
        {
            gate.Release();
        }
    });
    await Task.WhenAll(loads);
}

// Each label belongs to the map with the longest name that prefixes it ("CeladonCity_Gym_…" is the gym, not the city).
var byLength = maps.OrderByDescending(m => m.Length).ToList();
var trainersByMap = new Dictionary<string, HashSet<string>>();
foreach (var source in scriptSources)
{
    foreach (Match label in Regex.Matches(source, @"^(\w+)::\s*$(.*?)(?=^\w+::|\z)", RegexOptions.Multiline | RegexOptions.Singleline))
    {
        var battles = Regex.Matches(label.Groups[2].Value, @"trainerbattle\w*\s+(TRAINER_\w+)").Select(b => b.Groups[1].Value).ToList();
        if (battles.Count == 0)
        {
            continue;
        }

        var map = byLength.FirstOrDefault(m => label.Groups[1].Value.StartsWith(m + "_", StringComparison.Ordinal));
        if (map is not null)
        {
            var set = trainersByMap.TryGetValue(map, out var existing) ? existing : trainersByMap[map] = [];
            set.UnionWith(battles);
        }
    }
}

// Places.
var sectionNames = JsonNode.Parse((await Get("src/data/region_map/region_map_sections.json"))!)!["map_sections"]!.AsArray()
    .Where(s => s!["name"] is not null)
    .ToDictionary(s => (string)s!["id"]!, s => (string)s!["name"]!);
var places = new Dictionary<string, (string Pt, string En, SortedSet<string> Trainers, SortedSet<string> FireRed, SortedSet<string> LeafGreen)>();
(string Pt, string En, SortedSet<string> Trainers, SortedSet<string> FireRed, SortedSet<string> LeafGreen) Place(string map)
{
    var (_, section) = mapInfo[map];
    var name = PlaceName(sectionNames.GetValueOrDefault(section) ?? section.Replace("MAPSEC_", "").Replace('_', ' '));
    var (pt, en) = (name.Replace("Route ", "Rota ", StringComparison.Ordinal), name);
    if (map.EndsWith("_Gym", StringComparison.Ordinal) && trainersByMap.TryGetValue(map, out var gymTrainers)
        && gymTrainers.FirstOrDefault(t => trainers.TryGetValue(t, out var tr) && tr.Class == "LEADER") is { } leader)
    {
        var town = Regex.Replace(name, @" (City|Island|Town)$", "");
        var leaderName = TitleCase(trainers[leader].Name);
        (pt, en) = ($"Ginásio de {town} ({leaderName})", $"{town} Gym ({leaderName})");
    }

    return places.TryGetValue(pt, out var place) ? place : places[pt] = (pt, en, new(StringComparer.Ordinal), new(StringComparer.Ordinal), new(StringComparer.Ordinal));
}

foreach (var (map, battles) in trainersByMap)
{
    if (!mapInfo.ContainsKey(map))
    {
        continue;
    }

    var place = Place(map);
    foreach (var trainer in battles.SelectMany(t => (IEnumerable<string>)[t, .. rematches.GetValueOrDefault(t) ?? []]))
    {
        if (trainers.TryGetValue(trainer, out var tr) && parties.TryGetValue(tr.Party, out var team))
        {
            place.Trainers.UnionWith(team);
        }
    }
}

var wild = JsonNode.Parse((await Get("src/data/wild_encounters.json"))!)!;
var mapsById = mapInfo.ToDictionary(kv => kv.Value.Id, kv => kv.Key);
foreach (var encounter in wild["wild_encounter_groups"]![0]!["encounters"]!.AsArray())
{
    var label = (string)encounter!["base_label"]!;
    var fireRed = label.EndsWith("_FireRed", StringComparison.Ordinal);
    if ((!fireRed && !label.EndsWith("_LeafGreen", StringComparison.Ordinal))
        || !mapsById.TryGetValue((string)encounter["map"]!, out var map))
    {
        continue;
    }

    var place = Place(map);
    var target = fireRed ? place.FireRed : place.LeafGreen;
    foreach (var kind in new[] { "land_mons", "water_mons", "rock_smash_mons", "fishing_mons" })
    {
        foreach (var mon in encounter[kind]?["mons"]?.AsArray() ?? [])
        {
            target.Add(Species(((string)mon!["species"]!)["SPECIES_".Length..]));
        }
    }
}

var output = new JsonArray([
    .. places.Values
        .Where(p => p.Trainers.Count > 0 || p.FireRed.Count > 0 || p.LeafGreen.Count > 0)
        .OrderBy(p => p.En, StringComparer.Ordinal)
        .Select(p => (JsonNode)new JsonObject
        {
            ["pt"] = p.Pt,
            ["en"] = p.En,
            ["trainers"] = new JsonArray([.. p.Trainers.Select(s => (JsonNode)s)]),
            ["wild"] = new JsonObject
            {
                ["fireRed"] = new JsonArray([.. p.FireRed.Select(s => (JsonNode)s)]),
                ["leafGreen"] = new JsonArray([.. p.LeafGreen.Select(s => (JsonNode)s)]),
            },
        }),
]);
File.WriteAllText("locations.json", new JsonObject
{
    ["_fonte"] = $"pret/pokefirered@{Commit}: data/maps (map.json, scripts.inc), data/scripts/trainers.inc, src/data/trainers.h, src/data/trainer_parties.h, src/vs_seeker.c, src/data/wild_encounters.json (FireRed e LeafGreen), src/data/region_map/region_map_sections.json. Gerado por scripts/BuildLocations.cs.",
    ["places"] = output,
}.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n");
Console.WriteLine($"locations.json: {output.Count} places.");

static string Key(string name) => Regex.Replace(name.ToUpperInvariant(), "[^A-Z0-9]", "");

string Species(string decompName) =>
    speciesByKey.TryGetValue(Key(decompName), out var name)
        ? name
        : throw new InvalidOperationException($"Unknown species {decompName}");

static string PlaceName(string gameName) => gameName switch
{
    "S.S. ANNE" => "S.S. Anne",
    _ => TitleCase(gameName),
};

static string TitleCase(string text) => string.Join(' ', text.ToLowerInvariant()
    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
    .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
