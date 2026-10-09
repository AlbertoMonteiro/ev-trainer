using System.Text.Json;

namespace EvTrainer.Web.Localization;

/// <summary>UI strings per language, loaded from embedded <c>strings.&lt;tag&gt;.json</c> files.</summary>
public sealed class LocalizationCatalog
{
    public const string DefaultLanguage = "pt-BR";

    private const string ResourcePrefix = "EvTrainer.strings.";
    private const string ResourceSuffix = ".json";

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _languages;

    public LocalizationCatalog(IDictionary<string, IReadOnlyDictionary<string, string>> languages)
    {
        _languages = new Dictionary<string, IReadOnlyDictionary<string, string>>(languages, StringComparer.Ordinal);
        Languages =
        [
            .. _languages.Keys.Where(l => l == DefaultLanguage),
            .. _languages.Keys.Where(l => l != DefaultLanguage),
        ];
    }

    /// <summary>Available languages, default first.</summary>
    public IReadOnlyList<string> Languages { get; }

    public static LocalizationCatalog LoadEmbedded()
    {
        var assembly = typeof(LocalizationCatalog).Assembly;
        var languages = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        foreach (var name in assembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && n.EndsWith(ResourceSuffix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var document = JsonDocument.Parse(stream);
            var tag = name[ResourcePrefix.Length..^ResourceSuffix.Length];
            languages[tag] = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.GetString() ?? "", StringComparer.Ordinal);
        }

        return new LocalizationCatalog(languages);
    }

    public IReadOnlyDictionary<string, string> Strings(string language) =>
        _languages.TryGetValue(language, out var strings) ? strings : new Dictionary<string, string>();

    public string? Find(string language, string key) =>
        _languages.TryGetValue(language, out var strings) && strings.TryGetValue(key, out var text) ? text : null;
}