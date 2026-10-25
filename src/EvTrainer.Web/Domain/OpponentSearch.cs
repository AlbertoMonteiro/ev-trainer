using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>
/// Opponent search (HANDOFF §4.5): matches "name #dex", the wild places of the chosen game version or any trainer location,
/// ignoring accents and case, with route lists expanded and a digit boundary at the end of the query. Places match in
/// Portuguese or English whatever the UI language (spec 011); hits keep the Portuguese place as its identity.
/// Trainers of the training guide match by class, name or both (spec 016) and bring the species of their teams.
/// </summary>
public sealed class OpponentSearch
{
    private readonly IReadOnlyDictionary<GameVersion, IReadOnlyList<Entry>> _entries;
    private readonly IReadOnlyList<(GuideTrainer Trainer, string[] Prepared)> _trainers;

    public OpponentSearch(ReferenceData data, LocationNames names, VsSeekerGuide guide)
    {
        // Names are not route-expanded, so a trainer never matches a place ("Rota 11").
        _trainers =
        [
            .. guide.Trainers.Select(t => (t, new[] { TextFold.Fold($"{t.TrainerClass} {t.Name}"), TextFold.Fold(t.Name), TextFold.Fold(t.TrainerClass) })),
        ];

        Dictionary<string, List<string>> trainers = new(StringComparer.Ordinal);
        foreach (var location in data.TrainersByLocation)
        {
            foreach (var opponent in location.Opponents)
            {
                (trainers.TryGetValue(opponent, out var list) ? list : trainers[opponent] = []).Add(location.Name);
            }
        }

        // Wild places come from the game data of each version (spec 014); the curated note only places the vitamins.
        IReadOnlyList<Entry> Index(GameVersion version) =>
        [
            .. data.Opponents.Select(o => new Entry(
                o,
                Prepare($"{o.Name} #{o.Dex}"),
                o.IsVitamin
                    ? [.. names.AllForms(o.WildLocation).Select(Prepare)]
                    : [.. (data.WildPlaces(version).GetValueOrDefault(o.Name) ?? []).SelectMany(names.AllForms).Select(Prepare)],
                [.. (trainers.GetValueOrDefault(o.Name) ?? []).Select(l => (l, (IReadOnlyList<string>)[.. names.AllForms(l).Select(Prepare)]))])),
        ];

        _entries = Enum.GetValues<GameVersion>().ToDictionary(v => v, Index);
    }

    public IReadOnlyList<SearchHit> Find(string query, GameVersion version)
    {
        var folded = TextFold.Fold(query.Trim());
        if (folded.Length == 0)
        {
            return [];
        }

        // Species of the teams of the trainers found by name, with the trainer's place.
        Dictionary<string, List<string>> teams = new(StringComparer.Ordinal);
        foreach (var trainer in FindFolded(folded))
        {
            foreach (var (species, _) in trainer.Stages.SelectMany(s => s.Team))
            {
                (teams.TryGetValue(species, out var list) ? list : teams[species] = []).Add(trainer.Location);
            }
        }

        var hits = new List<SearchHit>();
        foreach (var entry in _entries[version])
        {
            List<string> trainerHits = [.. entry.Trainers.Where(t => t.Prepared.Any(p => Matches(p, folded))).Select(t => t.Name)];
            if (teams.TryGetValue(entry.Opponent.Name, out var places))
            {
                trainerHits = [.. trainerHits.Union(places)];
            }

            if (trainerHits.Count > 0 || Matches(entry.NameAndDex, folded) || entry.Wild.Any(w => Matches(w, folded)))
            {
                hits.Add(new SearchHit(entry.Opponent, trainerHits));
            }
        }

        return hits;
    }

    /// <summary>Trainers of the training guide whose class, name or "class name" matches, in guide order.</summary>
    public IReadOnlyList<GuideTrainer> FindTrainers(string query)
    {
        var folded = TextFold.Fold(query.Trim());
        return folded.Length == 0 ? [] : FindFolded(folded);
    }

    private List<GuideTrainer> FindFolded(string folded) =>
        [.. _trainers.Where(t => t.Prepared.Any(p => Matches(p, folded))).Select(t => t.Trainer)];

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

    private sealed record Entry(
        Opponent Opponent,
        string NameAndDex,
        IReadOnlyList<string> Wild,
        IReadOnlyList<(string Name, IReadOnlyList<string> Prepared)> Trainers);
}