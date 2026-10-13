using System.Text.Json;

namespace EvTrainer.Web.Domain.Reference;

/// <summary>
/// English names of the reference places (spec 011), from the embedded <c>reference-data.en.json</c>: the
/// Portuguese text in <c>reference-data.json</c> stays the place's identity; English is only for display and search.
/// </summary>
public sealed class LocationNames
{
    private const string ResourceName = "EvTrainer.reference-data.en.json";
    private const string LocationsResourceName = "EvTrainer.locations.json";

    private readonly IReadOnlyDictionary<string, string> _english;

    public LocationNames(IReadOnlyDictionary<string, string> english) => _english = english;

    /// <summary>Curated names (reference-data.en.json) plus every place generated from the game (locations.json).</summary>
    public static LocationNames LoadEmbedded()
    {
        var english = new Dictionary<string, string>(StringComparer.Ordinal);
        using (var curated = JsonDocument.Parse(Resource(ResourceName)))
        {
            foreach (var p in curated.RootElement.GetProperty("locations").EnumerateObject())
            {
                english[p.Name] = p.Value.GetString() ?? p.Name;
            }
        }

        using (var generated = JsonDocument.Parse(Resource(LocationsResourceName)))
        {
            foreach (var place in generated.RootElement.GetProperty("places").EnumerateArray())
            {
                english.TryAdd(place.GetProperty("pt").GetString()!, place.GetProperty("en").GetString()!);
            }
        }

        return new LocationNames(english);
    }

    private static Stream Resource(string name) =>
        typeof(LocationNames).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource {name} not found.");

    public bool HasTranslation(string place) => _english.ContainsKey(place);

    /// <summary>The place in the given language; Portuguese, or a place without translation, comes back as is.</summary>
    public string Display(string place, string language) =>
        language == LanguageMatcher.English && _english.TryGetValue(place, out var english) ? english : place;

    /// <summary>The place as written in every language (Portuguese first), without repeats.</summary>
    public IEnumerable<string> AllForms(string place) =>
        _english.TryGetValue(place, out var english) && english != place ? [place, english] : [place];
}