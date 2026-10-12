using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 009: useful opponents first, others after, each group keeping the original order.</summary>
public class UsefulFirstTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly int[] Targets = [252, 0, 2, 252, 0, 4];
    private static readonly int[] Totals = [0, 0, 2, 0, 0, 0];

    private static List<SearchHit> Hits(params string[] names) => [.. names.Select(n => new SearchHit(Data.Lookup(n)!))];

    private static string[] NamesOf(IEnumerable<SearchHit> hits) => [.. hits.Select(h => h.Opponent.Name)];

    [Fact]
    public void Useful_come_first_and_each_group_keeps_its_order()
    {
        var (useful, others) = Evs.UsefulFirst(Hits("Geodude", "Caterpie", "Machop", "Pidgey", "Ivysaur", "Oddish"), Totals, Targets);

        Assert.Equal(["Caterpie", "Pidgey", "Oddish"], NamesOf(useful));
        Assert.Equal(["Geodude", "Machop", "Ivysaur"], NamesOf(others));
    }

    [Fact]
    public void Trainer_locations_are_kept()
    {
        var hit = new SearchHit(Data.Lookup("Caterpie")!, ["Rota 4"]);

        var (useful, _) = Evs.UsefulFirst([hit], Totals, Targets);

        Assert.Same(hit, Assert.Single(useful));
    }

    [Theory]
    [InlineData(new[] { 0, 0, 0, 0, 0, 0 }, new[] { 0, 0, 0, 0, 0, 0 })]
    [InlineData(new[] { 252, 0, 2, 252, 0, 4 }, new[] { 252, 0, 2, 252, 0, 4 })]
    public void Without_open_targets_nothing_is_useful(int[] targets, int[] totals)
    {
        var (useful, others) = Evs.UsefulFirst(Hits("Caterpie", "Pidgey", "Calcium"), totals, targets);

        Assert.Empty(useful);
        Assert.Equal(["Caterpie", "Pidgey", "Calcium"], NamesOf(others));
    }

    [Fact]
    public void Vitamins_follow_the_useful_rule()
    {
        var (useful, others) = Evs.UsefulFirst(Hits("Calcium", "Protein"), [0, 0, 0, 100, 0, 0], [0, 252, 0, 252, 0, 0]);

        Assert.Equal(["Protein"], NamesOf(useful));
        Assert.Equal(["Calcium"], NamesOf(others));
    }

    [Fact]
    public void Every_opponent_lands_in_exactly_one_group()
    {
        var all = Data.Opponents.Select(o => new SearchHit(o)).ToList();

        var (useful, others) = Evs.UsefulFirst(all, Totals, Targets);

        Assert.Equal(all.Count, useful.Count + others.Count);
        Assert.Equal(NamesOf(all).Order(StringComparer.Ordinal), NamesOf(useful.Concat(others)).Order(StringComparer.Ordinal));
    }
}