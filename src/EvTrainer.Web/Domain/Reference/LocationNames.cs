using System.Text.Json;

namespace EvTrainer.Web.Domain.Reference;

/// <summary>
/// English names of the reference places (spec 011), from the embedded <c>reference-data.en.json</c>: the
/// Portuguese text in <c>reference-data.json</c> stays the place's identity; English is only for display and search.
/// </summary>
public sealed class LocationNames
{
    private const string ResourceName = "EvTrainer.reference-data.en.json";

    private readonly IReadOnlyDictionary<string, string> _english;

    public LocationNames(IReadOnlyDictionary<string, string> english) => _english = english;

    public static LocationNames LoadEmbedded()
    {
        using var stream = typeof(LocationNames).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");
        using var document = JsonDocument.Parse(stream);
        return new LocationNames(document.RootElement.GetProperty("locations").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString() ?? p.Name, StringComparer.Ordinal));
    }

    public bool HasTranslation(string place) => _english.ContainsKey(place);

    /// <summary>The place in the given language; Portuguese, or a place without translation, comes back as is.</summary>
    public string Display(string place, string language) =>
        language == LanguageMatcher.English && _english.TryGetValue(place, out var english) ? english : place;

    /// <summary>The place as written in every language (Portuguese first), without repeats.</summary>
    public IEnumerable<string> AllForms(string place) =>
        _english.TryGetValue(place, out var english) && english != place ? [place, english] : [place];
}