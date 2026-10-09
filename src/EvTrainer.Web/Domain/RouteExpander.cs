using System.Text.RegularExpressions;

namespace EvTrainer.Web.Domain;

/// <summary>"rota 3, 4, 22" → "rota 3, rota 4, rota 22"; "rotas 12–15" → "rota 12, rota 13, rota 14, rota 15".</summary>
public static partial class RouteExpander
{
    public static string Expand(string foldedText) =>
        RouteList().Replace(foldedText, match => string.Join(", ", Routes(match.Groups["list"].Value).Select(n => $"rota {n}")));

    private static IEnumerable<int> Routes(string list)
    {
        foreach (var part in ListSeparator().Split(list))
        {
            var bounds = RangeSeparator().Split(part).Select(int.Parse).ToArray();
            if (bounds.Length == 2)
            {
                for (var n = bounds[0]; n <= bounds[1]; n++)
                {
                    yield return n;
                }
            }
            else
            {
                yield return bounds[0];
            }
        }
    }

    [GeneratedRegex(@"rotas? (?<list>\d+(?:\s*[–-]\s*\d+)?(?:\s*,\s*\d+(?:\s*[–-]\s*\d+)?)*)")]
    private static partial Regex RouteList();

    [GeneratedRegex(@"\s*,\s*")]
    private static partial Regex ListSeparator();

    [GeneratedRegex(@"\s*[–-]\s*")]
    private static partial Regex RangeSeparator();
}