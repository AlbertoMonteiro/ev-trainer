using System.Globalization;

using Microsoft.Extensions.Localization;

namespace EvTrainer.Web.Localization;

/// <summary>
/// <see cref="IStringLocalizer"/> over the embedded JSON catalog: active language → pt-BR → "[key]".
/// </summary>
public class JsonStringLocalizer(LocalizationCatalog catalog, LanguageState language) : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            var text = catalog.Find(language.Current, name) ?? catalog.Find(LocalizationCatalog.DefaultLanguage, name);
            return text is null
                ? new LocalizedString(name, $"[{name}]", resourceNotFound: true)
                : new LocalizedString(name, text);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = this[name];
            return format.ResourceNotFound
                ? format
                : new LocalizedString(name, string.Format(CultureInfo.InvariantCulture, format.Value, arguments));
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        catalog.Strings(language.Current).Select(kv => new LocalizedString(kv.Key, kv.Value));
}

/// <summary>Typed variant so components can inject <c>IStringLocalizer&lt;T&gt;</c>.</summary>
public sealed class JsonStringLocalizer<T>(LocalizationCatalog catalog, LanguageState language)
    : JsonStringLocalizer(catalog, language), IStringLocalizer<T>;