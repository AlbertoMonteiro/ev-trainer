namespace EvTrainer.Web.Domain;

/// <summary>
/// Picks the UI language (spec 011): the saved choice when the app has it, otherwise the first browser
/// language the app supports (any pt-* → pt-BR, any en-* → en), otherwise English.
/// </summary>
public static class LanguageMatcher
{
    public const string Portuguese = "pt-BR";
    public const string English = "en";

    public static string Pick(IEnumerable<string> browserLanguages)
    {
        foreach (var tag in browserLanguages)
        {
            var primary = tag.Trim().Split('-')[0];
            if (primary.Equals("pt", StringComparison.OrdinalIgnoreCase))
            {
                return Portuguese;
            }

            if (primary.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                return English;
            }
        }

        return English;
    }

    public static string Resolve(string? saved, IReadOnlyCollection<string> available, IEnumerable<string> browserLanguages) =>
        saved is not null && available.Contains(saved) ? saved : Pick(browserLanguages);
}