// Generates vs-seeker-guide.json (Kanto and Sevii Islands trainers the VS Seeker can rematch, with every rematch stage)
// from the FireRed decompilation (pret/pokefirered). Run from the repository root:  dotnet run scripts/BuildVsSeekerGuide.cs
//
// src/vs_seeker.c, sRematches: per trainer, up to 6 trainer ids; the index is the rematch stage and decides when it
// becomes available (TryGetRematchTrainerIdGivenGameState): 1 = has the VS Seeker, 2 = visited Celadon, 3 = visited
// Fuchsia, 4 = beat the Elite Four, 5 = link with Ruby/Sapphire unlocked. SKIP (or the original id) = no new team.
// src/data/trainers.h: class, name, double battle and party; src/data/trainer_parties.h: species and levels.
// Places: the map's region map section (data/maps/*/map.json, src/data/region_map/region_map_sections.json), named like
// locations.json. Sevii Islands 1-3 open after Blaine, 4-7 after the Elite Four.
#:property TreatWarningsAsErrors=false
#:property PublishAot=false

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

const string Commit = "037335f4c725d7c9aecdac87066f2002b4bd7e14";
const string BaseUrl = $"https://raw.githubusercontent.com/pret/pokefirered/{Commit}/";

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
var vsSeeker = await http.GetStringAsync(BaseUrl + "src/vs_seeker.c");
var trainersSource = await http.GetStringAsync(BaseUrl + "src/data/trainers.h");
var partiesSource = await http.GetStringAsync(BaseUrl + "src/data/trainer_parties.h");

// Species names as the app knows them (Pokédex Gen III).
var pokedex = JsonNode.Parse(File.ReadAllText("pokedex-gen3.json"))!["species"]!.AsArray();
var speciesByKey = new Dictionary<string, string>();
foreach (var s in pokedex)
{
    speciesByKey.TryAdd(Key((string)s!["name"]!), (string)s!["name"]!);
}

// The ♀/♂ symbols vanish in the key, so the two Nidoran are mapped by hand.
speciesByKey["NIDORANF"] = "Nidoran♀";
speciesByKey["NIDORANM"] = "Nidoran♂";

// Trainers: [TRAINER_X] = { .trainerClass = ..., .trainerName = _("..."), .doubleBattle = ..., .party = MACRO(sParty_X) }
var trainers = new Dictionary<string, (string Class, string Name, bool Double, string Party)>();
foreach (Match m in Regex.Matches(trainersSource, @"\[(TRAINER_\w+)\] = \{(.*?)\n    \},", RegexOptions.Singleline))
{
    var body = m.Groups[2].Value;
    var cls = Regex.Match(body, @"trainerClass = TRAINER_CLASS_(\w+)");
    var party = Regex.Match(body, @"party = \w+\((sParty_\w+)\)");
    if (!cls.Success || !party.Success)
    {
        continue;
    }

    trainers[m.Groups[1].Value] = (
        cls.Groups[1].Value,
        Regex.Match(body, @"trainerName = _\(""([^""]*)""\)").Groups[1].Value,
        body.Contains("doubleBattle = TRUE", StringComparison.Ordinal),
        party.Groups[1].Value);
}

// Parties: static const struct ... sParty_X[] = { { .lvl = N, .species = SPECIES_Y, ... }, ... };
var parties = new Dictionary<string, List<(string Species, int Level)>>();
foreach (Match m in Regex.Matches(partiesSource, @"(sParty_\w+)\[\] = \{([^;]*)\};"))
{
    parties[m.Groups[1].Value] =
    [
        .. Regex.Matches(m.Groups[2].Value, @"\.lvl = (\d+),.*?\.species = SPECIES_(\w+)", RegexOptions.Singleline)
            .Select(p => (Species(p.Groups[2].Value), int.Parse(p.Groups[1].Value))),
    ];
}

// Map id (MAP_ONE_ISLAND_KINDLE_ROAD) → map folder (OneIsland_KindleRoad) → region map section name.
var mapGroups = JsonNode.Parse(await http.GetStringAsync(BaseUrl + "data/maps/map_groups.json"))!;
var folders = mapGroups["group_order"]!.AsArray()
    .SelectMany(g => mapGroups[(string)g!]!.AsArray().Select(m => (string)m!))
    .ToDictionary(Key);
var sectionNames = JsonNode.Parse(await http.GetStringAsync(BaseUrl + "src/data/region_map/region_map_sections.json"))!["map_sections"]!
    .AsArray()
    .Where(s => s!["name"] is not null)
    .ToDictionary(s => (string)s!["id"]!, s => (string)s!["name"]!);
var placeByMap = new Dictionary<string, string>();
async Task<string> Place(string mapId)
{
    if (!placeByMap.TryGetValue(mapId, out var place))
    {
        var folder = folders[Key(mapId["MAP_".Length..])];
        var section = (string)JsonNode.Parse(await http.GetStringAsync(BaseUrl + $"data/maps/{folder}/map.json"))!["region_map_section"]!;
        place = placeByMap[mapId] = TitleCase(sectionNames[section]).Replace("Route ", "Rota ", StringComparison.Ordinal);
    }

    return place;
}

var table = vsSeeker[vsSeeker.IndexOf("sRematches[] = {", StringComparison.Ordinal)..];
table = table[..table.IndexOf("};", StringComparison.Ordinal)];

var output = new JsonArray();
foreach (Match m in Regex.Matches(table, @"\{\s*\{([^{}]*)\},\s*MAP\((\w+)\)\s*\}"))
{
    var map = m.Groups[2].Value;
    var route = Regex.Match(map, @"^MAP_ROUTE(\d+)(?:_NORTH|_SOUTH)?$");
    var island = Regex.Match(map, @"^MAP_(ONE|TWO|THREE|FOUR|FIVE|SIX|SEVEN)_ISLAND_");
    if (!route.Success && !island.Success)
    {
        throw new InvalidOperationException($"Unexpected rematch map {map}");
    }

    var ids = m.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    var (cls, name, isDouble, _) = trainers[ids[0]];

    var stages = new JsonArray();
    List<(string, int)>? previous = null;
    for (var index = 0; index < ids.Length; index++)
    {
        if (ids[index] == "SKIP" || (index > 0 && ids[index] == ids[0]))
        {
            continue;
        }

        var team = parties[trainers[ids[index]].Party];
        if (team.Count == 0)
        {
            throw new InvalidOperationException($"Empty party for {ids[index]}");
        }

        if (previous is not null && team.SequenceEqual(previous))
        {
            continue;
        }

        previous = team;
        stages.Add(new JsonObject
        {
            ["index"] = index,
            ["team"] = new JsonArray([.. team.Select(p => (JsonNode)new JsonArray(p.Item1, p.Item2))]),
        });
    }

    // Sevii: swimmers are always in the water.
    JsonArray access = island.Success
        ? [island.Groups[1].Value is "ONE" or "TWO" or "THREE" ? "sevii" : "seviiPostgame", .. cls.StartsWith("SWIMMER", StringComparison.Ordinal) ? new[] { "surf" } : []]
        : int.Parse(route.Groups[1].Value) switch
        {
            19 or 20 or 21 => ["surf"],
            17 => ["cyclingRoad"],
            23 => ["victoryRoad"],
            _ => [],
        };

    output.Add(new JsonObject
    {
        ["id"] = ids[0]["TRAINER_".Length..],
        ["class"] = ClassName(cls),
        ["name"] = TitleCase(name),
        ["location"] = await Place(map),
        ["double"] = isDouble,
        ["access"] = access,
        ["stages"] = stages,
    });
}

var document = new JsonObject
{
    ["_fonte"] = $"pret/pokefirered@{Commit}: src/vs_seeker.c (sRematches), src/data/trainers.h, src/data/trainer_parties.h, data/maps (map.json), src/data/region_map/region_map_sections.json. Gerado por scripts/BuildVsSeekerGuide.cs.",
    ["trainers"] = output,
};
File.WriteAllText("vs-seeker-guide.json", document.ToJsonString(new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
}) + "\n");
Console.WriteLine($"vs-seeker-guide.json: {output.Count} trainers.");

static string Key(string name) => Regex.Replace(name.ToUpperInvariant(), "[^A-Z0-9]", "");

string Species(string decompName) =>
    speciesByKey.TryGetValue(Key(decompName), out var name)
        ? name
        : throw new InvalidOperationException($"Unknown species {decompName}");

static string ClassName(string cls) => cls switch
{
    "PKMN_RANGER" => "Pokémon Ranger",
    "PKMN_BREEDER" => "Pokémon Breeder",
    "POKEMANIAC" => "PokéManiac",
    "SWIMMER_M" or "SWIMMER_F" => "Swimmer",
    "SIS_AND_BRO" => "Sis and Bro",
    _ => TitleCase(cls.Replace('_', ' ')),
};

static string TitleCase(string text) => string.Join(' ', text.ToLowerInvariant()
    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
    .Select(word => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]));
