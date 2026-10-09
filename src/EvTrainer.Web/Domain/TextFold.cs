using System.Text;

namespace EvTrainer.Web.Domain;

/// <summary>
/// Search normalization without ICU (research R7): lower case, accents removed by a closed table,
/// ♀ → " f" and ♂ → " m".
/// </summary>
public static class TextFold
{
    private static readonly Dictionary<char, string> Map = new()
    {
        ['á'] = "a",
        ['à'] = "a",
        ['â'] = "a",
        ['ã'] = "a",
        ['ä'] = "a",
        ['é'] = "e",
        ['è'] = "e",
        ['ê'] = "e",
        ['ë'] = "e",
        ['í'] = "i",
        ['ì'] = "i",
        ['î'] = "i",
        ['ï'] = "i",
        ['ó'] = "o",
        ['ò'] = "o",
        ['ô'] = "o",
        ['õ'] = "o",
        ['ö'] = "o",
        ['ú'] = "u",
        ['ù'] = "u",
        ['û'] = "u",
        ['ü'] = "u",
        ['ç'] = "c",
        ['ñ'] = "n",
        ['’'] = "'",
        ['♀'] = " f",
        ['♂'] = " m",
    };

    public static string Fold(string text)
    {
        var builder = new StringBuilder(text.Length + 4);
        foreach (var c in text.ToLowerInvariant())
        {
            if (Map.TryGetValue(c, out var replacement))
            {
                builder.Append(replacement);
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}