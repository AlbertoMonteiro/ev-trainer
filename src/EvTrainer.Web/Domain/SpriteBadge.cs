using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>
/// What to draw in place of (and under) a Pokémon sprite: the Pokédex number, the stats it gives EVs in
/// (for the badge color) and the initial used when there is no number.
/// </summary>
public sealed record SpriteBadge(int? Dex, IReadOnlyList<int> Stats, string Initial)
{
    private const int LastSpecies = 386;
    private const int MaxColors = 2;

    public bool HasSprite => Dex is >= 1 and <= LastSpecies;

    public static SpriteBadge For(string name, ReferenceData data, Pokedex dex)
    {
        var canonical = data.Canon(name);
        var opponent = data.Lookup(canonical);
        var number = dex.Find(canonical)?.Dex ?? opponent?.Dex;
        int[] stats = opponent is null
            ? []
            : [.. opponent.EvYield
                .Select((yield, index) => (yield, index))
                .Where(s => s.yield > 0)
                .OrderByDescending(s => s.yield)
                .ThenBy(s => s.index)
                .Take(MaxColors)
                .Select(s => s.index)
                .Order()];
        var initial = canonical.Length > 0 ? canonical[..1].ToUpperInvariant() : "?";
        return new SpriteBadge(number, stats, initial);
    }
}