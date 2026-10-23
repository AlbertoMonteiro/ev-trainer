// Generates league.json (Elite Four and Champion teams, every move and what each species can learn in FireRed/LeafGreen)
// from the FireRed decompilation (pret/pokefirered). Run from the repository root:  dotnet run scripts/BuildLeague.cs
//
// src/data/trainers.h + trainer_parties.h: TRAINER_ELITE_FOUR_<NAME> (first battle), TRAINER_ELITE_FOUR_<NAME>_2
// (rematch), TRAINER_CHAMPION_{FIRST,REMATCH}_<RIVAL STARTER>; all with custom moves and held items. The rival takes the
// starter that beats the player's: player Bulbasaur → rival Charmander, Charmander → Squirtle, Squirtle → Bulbasaur
// (data/maps/PalletTown_ProfessorOaksLab/scripts.inc). Trainer Pokémon always have the species' first ability: their
// personality value is always even (CreateNPCTrainerParty in src/battle_main.c).
// src/data/battle_moves.h: type, power, accuracy, PP; power 0 = status, power 1 = fixed or variable damage (by effect).
// Curse has the "???" (TYPE_MYSTERY) type.
// src/data/pokemon/species_info.h: types and abilities. level_up_learnsets.h (+ pointers), tmhm_learnsets.h and
// tutor_learnsets.h: what each species learns; Frenzy Plant, Blast Burn and Hydro Cannon only for the final starters
// (src/party_menu.c).
#:property TreatWarningsAsErrors=false
#:property PublishAot=false

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

const string Commit = "037335f4c725d7c9aecdac87066f2002b4bd7e14";
const string BaseUrl = $"https://raw.githubusercontent.com/pret/pokefirered/{Commit}/";

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
async Task<string> Get(string path) => await http.GetStringAsync(BaseUrl + path);

var trainersSource = await Get("src/data/trainers.h");
var partiesSource = await Get("src/data/trainer_parties.h");
var movesSource = await Get("src/data/battle_moves.h");
var speciesSource = await Get("src/data/pokemon/species_info.h");
var levelUpSource = await Get("src/data/pokemon/level_up_learnsets.h");
var levelUpPointers = await Get("src/data/pokemon/level_up_learnset_pointers.h");
var tmhmSource = await Get("src/data/pokemon/tmhm_learnsets.h");
var tutorSource = await Get("src/data/pokemon/tutor_learnsets.h");

// Species names as the app knows them (Pokédex Gen III), by normalized decompilation name.
var pokedex = JsonNode.Parse(File.ReadAllText("pokedex-gen3.json"))!["species"]!.AsArray();
var speciesByKey = new Dictionary<string, string>();
foreach (var s in pokedex)
{
    speciesByKey.TryAdd(Key((string)s!["name"]!), (string)s!["name"]!);
}

speciesByKey["NIDORANF"] = "Nidoran♀";
speciesByKey["NIDORANM"] = "Nidoran♂";

// Moves.
var fixedEffects = new HashSet<string> { "DRAGON_RAGE", "SONICBOOM", "LEVEL_DAMAGE", "SUPER_FANG", "PSYWAVE", "ENDEAVOR", "OHKO" };
var moves = new Dictionary<string, (string Name, string Type, int Power, int Accuracy, int Pp, string Damage)>();
foreach (Match m in Regex.Matches(movesSource, @"\[MOVE_(\w+)\] =\s*\{(.*?)\n    \},", RegexOptions.Singleline))
{
    if (m.Groups[1].Value == "NONE")
    {
        continue;
    }

    var body = m.Groups[2].Value;
    var effect = Regex.Match(body, @"\.effect = EFFECT_(\w+)").Groups[1].Value;
    var power = int.Parse(Regex.Match(body, @"\.power = (\d+)").Groups[1].Value);
    var damage = power switch
    {
        0 => "status",
        1 => fixedEffects.Contains(effect) ? "fixed" : "variable",
        _ => "normal",
    };
    moves[m.Groups[1].Value] = (
        MoveName(m.Groups[1].Value),
        Regex.Match(body, @"\.type = TYPE_(\w+)").Groups[1].Value is "MYSTERY" ? "???" : TitleCase(Regex.Match(body, @"\.type = TYPE_(\w+)").Groups[1].Value),
        power,
        int.Parse(Regex.Match(body, @"\.accuracy = (\d+)").Groups[1].Value),
        int.Parse(Regex.Match(body, @"\.pp = (\d+)").Groups[1].Value),
        damage);
}

string Move(string id) => moves.TryGetValue(id, out var move) ? move.Name : throw new InvalidOperationException($"Unknown move {id}");

// Species: types and abilities.
var speciesData = new Dictionary<string, (string[] Types, string[] Abilities)>();
foreach (var chunk in speciesSource.Split("[SPECIES_").Skip(1))
{
    var id = chunk[..chunk.IndexOf(']')];
    var types = Regex.Match(chunk, @"\.types = \{TYPE_(\w+), TYPE_(\w+)\}");
    var abilities = Regex.Match(chunk, @"\.abilities = \{ABILITY_(\w+), ABILITY_(\w+)\}");
    if (!types.Success || !abilities.Success || !speciesByKey.TryGetValue(Key(id), out var name))
    {
        continue;
    }

    speciesData[name] = (
        [.. new[] { types.Groups[1].Value, types.Groups[2].Value }.Distinct().Select(TitleCase)],
        [.. new[] { abilities.Groups[1].Value, abilities.Groups[2].Value }.Where(a => a != "NONE").Distinct().Select(AbilityName)]);
}

// Level-up learnsets.
var levelUpArrays = new Dictionary<string, List<(int Level, string Move)>>();
foreach (Match m in Regex.Matches(levelUpSource, @"(s\w+LevelUpLearnset)\[\] = \{(.*?)\};", RegexOptions.Singleline))
{
    levelUpArrays[m.Groups[1].Value] =
    [
        .. Regex.Matches(m.Groups[2].Value, @"LEVEL_UP_MOVE\(\s*(\d+),\s*MOVE_(\w+)\)")
            .Select(l => (int.Parse(l.Groups[1].Value), Move(l.Groups[2].Value))),
    ];
}

// Per species: level-up [level, move], TM/HM labels, tutor moves.
var levelMoves = new Dictionary<string, List<(int Level, string Move)>>();
var tmLabels = new Dictionary<string, List<string>>();
var tutorMoves = new Dictionary<string, List<string>>();
var tms = new SortedDictionary<string, string>(StringComparer.Ordinal);
static List<T> ListOf<T>(Dictionary<string, List<T>> lists, string name) => lists.TryGetValue(name, out var list) ? list : lists[name] = [];

foreach (Match m in Regex.Matches(levelUpPointers, @"\[SPECIES_(\w+)\] = (s\w+LevelUpLearnset)"))
{
    if (m.Groups[1].Value == "NONE" || !speciesByKey.TryGetValue(Key(m.Groups[1].Value), out var name))
    {
        continue;
    }

    var seen = new HashSet<(int, string)>();
    foreach (var (level, move) in levelUpArrays[m.Groups[2].Value].OrderBy(l => l.Level))
    {
        if (seen.Add((level, move)))
        {
            ListOf(levelMoves, name).Add((level, move));
        }
    }
}

// TM/HM learnsets: TMHM(TM24_THUNDERBOLT).
foreach (Match m in Regex.Matches(tmhmSource, @"\[SPECIES_(\w+)\]\s*= TMHM_LEARNSET\((.*?)\),", RegexOptions.Singleline))
{
    if (m.Groups[1].Value == "NONE" || !speciesByKey.TryGetValue(Key(m.Groups[1].Value), out var name))
    {
        continue;
    }

    foreach (Match t in Regex.Matches(m.Groups[2].Value, @"TMHM\(((?:TM|HM)\d\d)_(\w+)\)").OrderBy(t => t.Groups[1].Value.StartsWith('H')).ThenBy(t => t.Groups[1].Value))
    {
        tms[t.Groups[1].Value] = Move(t.Groups[2].Value);
        ListOf(tmLabels, name).Add(t.Groups[1].Value);
    }
}

// Tutor learnsets: TUTOR(MOVE_BODY_SLAM), plus the starters' ultimate moves.
foreach (var chunk in tutorSource[tutorSource.IndexOf("sTutorLearnsets[]", StringComparison.Ordinal)..].Split("[SPECIES_").Skip(1))
{
    var id = chunk[..chunk.IndexOf(']')];
    if (id == "NONE" || !speciesByKey.TryGetValue(Key(id), out var name))
    {
        continue;
    }

    foreach (Match t in Regex.Matches(chunk, @"TUTOR\(MOVE_(\w+)\)"))
    {
        ListOf(tutorMoves, name).Add(Move(t.Groups[1].Value));
    }
}

foreach (var (species, move) in new[] { ("Venusaur", "FRENZY_PLANT"), ("Charizard", "BLAST_BURN"), ("Blastoise", "HYDRO_CANNON") })
{
    ListOf(tutorMoves, species).Add(Move(move));
}

// Trainers.
var trainers = new Dictionary<string, string>();
foreach (Match m in Regex.Matches(trainersSource, @"\[(TRAINER_\w+)\] = \{(.*?)\n    \},", RegexOptions.Singleline))
{
    var party = Regex.Match(m.Groups[2].Value, @"party = \w+\((sParty_\w+)\)");
    if (party.Success)
    {
        trainers[m.Groups[1].Value] = party.Groups[1].Value;
    }
}

var parties = new Dictionary<string, string>();
foreach (Match m in Regex.Matches(partiesSource, @"(sParty_\w+)\[\] = \{([^;]*)\};"))
{
    parties[m.Groups[1].Value] = m.Groups[2].Value;
}

JsonArray Team(string trainer)
{
    var party = parties[trainers[trainer]];
    var team = new JsonArray();
    foreach (Match p in Regex.Matches(party, @"\.lvl = (\d+),\s*\.species = SPECIES_(\w+),\s*\.heldItem = ITEM_(\w+),\s*\.moves = \{([^}]*)\}"))
    {
        var name = speciesByKey[Key(p.Groups[2].Value)];
        team.Add(new JsonObject
        {
            ["species"] = name,
            ["level"] = int.Parse(p.Groups[1].Value),
            ["item"] = p.Groups[3].Value == "NONE" ? null : TitleCase(p.Groups[3].Value.Replace('_', ' ')),
            ["ability"] = speciesData[name].Abilities[0],
            ["moves"] = new JsonArray([.. p.Groups[4].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(id => id != "MOVE_NONE").Select(id => (JsonNode)Move(id["MOVE_".Length..]))]),
        });
    }

    return team.Count > 0 ? team : throw new InvalidOperationException($"No party for {trainer}");
}

var league = new JsonArray();
foreach (var member in new[] { "LORELEI", "BRUNO", "AGATHA", "LANCE" })
{
    league.Add(new JsonObject
    {
        ["id"] = member.ToLowerInvariant(),
        ["name"] = TitleCase(member),
        ["teams"] = new JsonObject
        {
            ["first"] = Team($"TRAINER_ELITE_FOUR_{member}"),
            ["rematch"] = Team($"TRAINER_ELITE_FOUR_{member}_2"),
        },
    });
}

// Player's starter → rival's starter.
var rivalOf = new Dictionary<string, string> { ["bulbasaur"] = "CHARMANDER", ["charmander"] = "SQUIRTLE", ["squirtle"] = "BULBASAUR" };
var championTeams = new JsonObject();
foreach (var round in new[] { ("first", "FIRST"), ("rematch", "REMATCH") })
{
    foreach (var (player, rival) in rivalOf)
    {
        championTeams[$"{round.Item1}.{player}"] = Team($"TRAINER_CHAMPION_{round.Item2}_{rival}");
    }
}

league.Add(new JsonObject { ["id"] = "champion", ["name"] = "Rival", ["teams"] = championTeams });

// Only moves someone can learn or use.
var used = new HashSet<string>([.. levelMoves.Values.SelectMany(l => l.Select(e => e.Move)), .. tms.Values, .. tutorMoves.Values.SelectMany(l => l)]);
foreach (var mon in league.SelectMany(t => t!["teams"]!.AsObject().SelectMany(team => team.Value!.AsArray())))
{
    foreach (var move in mon!["moves"]!.AsArray())
    {
        used.Add((string)move!);
    }
}

var movesJson = new JsonObject();
foreach (var move in moves.Values.Where(m => used.Contains(m.Name)).OrderBy(m => m.Name, StringComparer.Ordinal))
{
    movesJson[move.Name] = new JsonObject
    {
        ["type"] = move.Type,
        ["power"] = move.Power,
        ["accuracy"] = move.Accuracy,
        ["pp"] = move.Pp,
        ["damage"] = move.Damage,
    };
}

var speciesJson = new JsonObject();
foreach (var s in pokedex)
{
    var name = (string)s!["name"]!;
    var (types, abilities) = speciesData.TryGetValue(name, out var data) ? data : throw new InvalidOperationException($"No species info for {name}");
    speciesJson[name] = new JsonObject
    {
        ["types"] = new JsonArray([.. types.Select(t => (JsonNode)t)]),
        ["abilities"] = new JsonArray([.. abilities.Select(a => (JsonNode)a)]),
        ["level"] = new JsonArray([.. ListOf(levelMoves, name).Select(e => (JsonNode)new JsonArray(e.Level, e.Move))]),
        ["tm"] = new JsonArray([.. ListOf(tmLabels, name).Select(t => (JsonNode)t)]),
        ["tutor"] = new JsonArray([.. ListOf(tutorMoves, name).Select(t => (JsonNode)t)]),
    };
}

var document = new JsonObject
{
    ["_fonte"] = $"pret/pokefirered@{Commit}: src/data/trainers.h, src/data/trainer_parties.h, src/data/battle_moves.h, src/data/pokemon/species_info.h, level_up_learnsets.h, tmhm_learnsets.h, tutor_learnsets.h, src/party_menu.c. Gerado por scripts/BuildLeague.cs.",
    ["moves"] = movesJson,
    ["tms"] = new JsonObject([.. tms.Select(t => KeyValuePair.Create(t.Key, (JsonNode?)t.Value))]),
    ["species"] = speciesJson,
    ["trainers"] = league,
};
File.WriteAllText("league.json", document.ToJsonString(new JsonSerializerOptions
{
    WriteIndented = false,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
}) + "\n");
Console.WriteLine($"league.json: {movesJson.Count} moves, {speciesJson.Count} species, {league.Count} trainers.");

static string Key(string name) => Regex.Replace(name.ToUpperInvariant(), "[^A-Z0-9]", "");

static string TitleCase(string text) => string.Join(' ', text.ToLowerInvariant()
    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
    .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

static string MoveName(string id) => id switch
{
    "DOUBLE_EDGE" => "Double-Edge",
    "SELF_DESTRUCT" => "Self-Destruct",
    "SOFT_BOILED" => "Soft-Boiled",
    "WILL_O_WISP" => "Will-O-Wisp",
    "LOCK_ON" => "Lock-On",
    "MUD_SLAP" => "Mud-Slap",
    "X_SCISSOR" => "X-Scissor",
    _ => TitleCase(id.Replace('_', ' ')),
};

static string AbilityName(string id) => TitleCase(id.Replace('_', ' '));
