using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>
/// Opponent search (HANDOFF §4.5): matches "name #dex", the wild location or any trainer location,
/// ignoring accents and case, with route lists expanded and a digit boundary at the end of the query.
/// </summary>
public sealed class OpponentSearch
{
    private readonly IReadOnlyList<Entry> _entries;

    public OpponentSearch(ReferenceData data)
    {
        Dictionary<string, List<string>> trainers = new(StringComparer.Ordinal);
        foreach (var location in data.TrainersByLocation)
        {
            foreach (var opponent in location.Opponents)
            {
                (trainers.TryGetValue(opponent, out var list) ? list : trainers[opponent] = []).Add(location.Name);
            }
        }

        _entries =
        [
            .. data.Opponents.Select(o => new Entry(
                o,
                Prepare($"{o.Name} #{o.Dex}"),
                Prepare(o.WildLocation),
                [.. (trainers.GetValueOrDefault(o.Name) ?? []).Select(l => (l, Prepare(l)))])),
        ];
    }

    public IReadOnlyList<SearchHit> Find(string query)
    {
        var folded = TextFold.Fold(query.Trim());
        if (folded.Length == 0)
        {
            return [];
        }

        var hits = new List<SearchHit>();
        foreach (var entry in _entries)
        {
            List<string> trainerHits = [.. entry.Trainers.Where(t => Matches(t.Prepared, folded)).Select(t => t.Name)];
            if (trainerHits.Count > 0 || Matches(entry.NameAndDex, folded) || Matches(entry.Wild, folded))
            {
                hits.Add(new SearchHit(entry.Opponent, trainerHits));
            }
        }

        return hits;
    }

    private static string Prepare(string text) => RouteExpander.Expand(TextFold.Fold(text));

    /// <summary>Substring match; when the query ends in a digit the next character must not be a digit.</summary>
    private static bool Matches(string text, string query)
    {
        var endsWithDigit = char.IsAsciiDigit(query[^1]);
        for (var i = text.IndexOf(query, StringComparison.Ordinal); i >= 0; i = text.IndexOf(query, i + 1, StringComparison.Ordinal))
        {
            var next = i + query.Length;
            if (!endsWithDigit || next >= text.Length || !char.IsAsciiDigit(text[next]))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record Entry(Opponent Opponent, string NameAndDex, string Wild, IReadOnlyList<(string Name, string Prepared)> Trainers);
}