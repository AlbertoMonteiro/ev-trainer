// Generates pokedex-gen3.json (species #1–386 with Generation III base stats and evolution links)
// from PokéAPI. Run from the repository root:  dotnet run scripts/BuildPokedex.cs
//
// Base stats changed in later generations (e.g. Butterfree SpAtk 80 → 90 in Gen VI). PokéAPI keeps
// the old values in "past_stats": an entry for generation G holds the value used up to G, so the
// Gen III value is the past entry with the smallest G >= 3, or the current value when there is none.
#:property TreatWarningsAsErrors=false
#:property PublishAot=false

using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

const int LastSpecies = 386;
const int TargetGeneration = 3;
string[] statOrder = ["hp", "attack", "defense", "special-attack", "special-defense", "speed"];

using var http = new HttpClient { BaseAddress = new Uri("https://pokeapi.co/api/v2/"), Timeout = TimeSpan.FromSeconds(60) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("ev-trainer-pokedex-builder/1.0");

// Names used by the app for Kanto (♀/♂, apostrophes) come from reference-data.json.
var reference = JsonNode.Parse(File.ReadAllText("reference-data.json"))!;
var kantoNames = reference["opponents"]!.AsArray()
    .Where(o => o!["dex"] is not null)
    .ToDictionary(o => (int)o!["dex"]!, o => (string)o!["name"]!);

// Species, English names and evolution links in one GraphQL call.
var graph = await new HttpClient().PostAsJsonAsync("https://beta.pokeapi.co/graphql/v1beta", new
{
    query = $$$"""
        query {
          s: pokemon_v2_pokemonspecies(where: {id: {_lte: {{{LastSpecies}}}}}, order_by: {id: asc}) {
            id evolves_from_species_id
            names: pokemon_v2_pokemonspeciesnames(where: {language_id: {_eq: 9}}) { name }
          }
        }
        """,
});
var species = JsonNode.Parse(await graph.Content.ReadAsStringAsync())!["data"]!["s"]!.AsArray();

var output = new JsonArray();
using var throttle = new SemaphoreSlim(8);
var stats = new int[LastSpecies + 1][];
await Task.WhenAll(Enumerable.Range(1, LastSpecies).Select(async id =>
{
    await throttle.WaitAsync();
    try
    {
        var pokemon = JsonNode.Parse(await http.GetStringAsync($"pokemon/{id}"))!;
        var current = pokemon["stats"]!.AsArray()
            .ToDictionary(s => (string)s!["stat"]!["name"]!, s => (int)s!["base_stat"]!);
        foreach (var past in pokemon["past_stats"]!.AsArray()
                     .Select(p => (Generation: GenerationNumber((string)p!["generation"]!["name"]!), Stats: p["stats"]!.AsArray()))
                     .Where(p => p.Generation >= TargetGeneration)
                     .OrderByDescending(p => p.Generation))
        {
            foreach (var stat in past.Stats)
            {
                current[(string)stat!["stat"]!["name"]!] = (int)stat["base_stat"]!;
            }
        }

        stats[id] = [.. statOrder.Select(s => current[s])];
    }
    finally
    {
        throttle.Release();
    }
}));

foreach (var s in species)
{
    var id = (int)s!["id"]!;
    var name = kantoNames.GetValueOrDefault(id) ?? ((string)s["names"]![0]!["name"]!).Replace('’', '\'');
    output.Add(new JsonObject
    {
        ["dex"] = id,
        ["name"] = name,
        ["base"] = new JsonArray([.. stats[id].Select(v => (JsonNode)v)]),
        // Pre-evolutions added after Gen III (e.g. Mime Jr. → Mr. Mime) don't exist in FireRed.
        ["evolvesFrom"] = s["evolves_from_species_id"]?.GetValue<int>() is int from && from <= LastSpecies ? from : null,
    });
}

var document = new JsonObject
{
    ["_fonte"] = "PokéAPI (pokeapi.co), stats base da Geração III (past_stats aplicados). Gerado por scripts/BuildPokedex.cs.",
    ["stats"] = new JsonArray("HP", "Ataque", "Defesa", "SpAtk", "SpDef", "Speed"),
    ["species"] = output,
};

File.WriteAllText("pokedex-gen3.json", document.ToJsonString(new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
}) + Environment.NewLine);
Console.WriteLine($"pokedex-gen3.json: {output.Count} species");

static int GenerationNumber(string name) => name["generation-".Length..] switch
{
    "i" => 1, "ii" => 2, "iii" => 3, "iv" => 4, "v" => 5, "vi" => 6, "vii" => 7, "viii" => 8, "ix" => 9,
    var other => throw new InvalidOperationException($"Unknown generation '{other}'"),
};
