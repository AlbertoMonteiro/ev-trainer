using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.Domain;

/// <summary>
/// The current party (spec 016): at most <see cref="MaxSize"/> Pokémon in training are in it, the others are stored.
/// </summary>
public static class Party
{
    public const int MaxSize = 6;

    /// <summary>Party ids in the user's order (Order, then id).</summary>
    public static IReadOnlyList<string> Members(IReadOnlyDictionary<string, TrainingPokemon> all) =>
        [.. all.Where(kv => kv.Value.Party == true)
            .OrderBy(kv => kv.Value.Order)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key)];

    /// <summary>
    /// Makes the party consistent after a load, an import or a sync (research R1): at most six marked, older data
    /// (null) fills the free places from the most recently changed, the rest is stored, and a stored Pokémon never
    /// holds the Exp. Share. Returns the ids changed; never stamps (any device redoes it the same way).
    /// </summary>
    public static string[] Normalize(IDictionary<string, TrainingPokemon> all)
    {
        var members = ByRecency(all.Where(kv => kv.Value.Party == true)).ToList();
        var unknown = ByRecency(all.Where(kv => kv.Value.Party is null)).ToList();
        var free = Math.Max(0, MaxSize - members.Count);
        HashSet<string> keep = [.. members.Take(MaxSize), .. unknown.Take(free)];

        List<string> changed = [];
        foreach (var (id, pokemon) in all)
        {
            var party = keep.Contains(id);
            var holds = pokemon.ExpShare && party;
            if (pokemon.Party != party || pokemon.ExpShare != holds)
            {
                pokemon.Party = party;
                pokemon.ExpShare = holds;
                changed.Add(id);
            }
        }

        return [.. changed];
    }

    private static IEnumerable<string> ByRecency(IEnumerable<KeyValuePair<string, TrainingPokemon>> pokemon) =>
        pokemon.OrderByDescending(kv => kv.Value.UpdatedAt ?? DateTimeOffset.MinValue)
            .ThenBy(kv => kv.Value.Order)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key);
}