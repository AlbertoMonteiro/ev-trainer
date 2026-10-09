using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class EvTotalsTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    private static int[] Totals(params (string Opponent, int Count, int Multiplier)[] events) =>
        Evs.Totals(Pokemons.Squirtle().WithEvents(events).Events, Data);

    [Fact]
    public void Handoff_limits_case()
    {
        Assert.Equal([255, 0, 0, 100, 0, 0], Totals(("Bulbasaur", 1, 1), ("Calcium", 12, 1), ("Caterpie", 300, 2)));
    }

    [Fact]
    public void Handoff_alias_case()
    {
        Assert.Equal([0, 0, 2, 1, 0, 0], Totals(("Bulbasaur (rival)", 1, 1), ("Geodude", 1, 1), ("Onix", 1, 1)));
    }

    [Fact]
    public void Vitamin_only_fills_up_to_100()
    {
        Assert.Equal([0, 0, 0, 95, 0, 0], Totals(("Abra", 95, 1)));
        Assert.Equal([0, 0, 0, 100, 0, 0], Totals(("Abra", 95, 1), ("Calcium", 1, 1)));
        Assert.Equal([0, 0, 0, 120, 0, 0], Totals(("Abra", 120, 1), ("Calcium", 3, 1)));
    }

    [Fact]
    public void Vitamin_ignores_the_multiplier()
    {
        Assert.Equal([10, 0, 0, 0, 0, 0], Totals(("HP Up", 1, 4)));
    }

    [Fact]
    public void Order_matters_vitamin_after_battles_may_not_work()
    {
        Assert.Equal([0, 0, 0, 110, 0, 0], Totals(("Calcium", 1, 1), ("Abra", 100, 1)));
        Assert.Equal([0, 0, 0, 100, 0, 0], Totals(("Abra", 100, 1), ("Calcium", 1, 1)));
    }

    [Fact]
    public void Total_never_exceeds_510()
    {
        var totals = Totals(("Caterpie", 255, 1), ("Abra", 255, 1), ("Pidgey", 255, 1));

        Assert.Equal(510, totals.Sum());
        Assert.Equal([255, 0, 0, 255, 0, 0], totals);
    }

    [Fact]
    public void Unknown_event_adds_nothing()
    {
        Assert.Equal([1, 0, 0, 0, 0, 0], Totals(("Agumon", 5, 1), ("Caterpie", 1, 1)));
    }

    [Fact]
    public void Multipliers_apply_per_event()
    {
        Assert.Equal([7, 0, 0, 0, 0, 0], Totals(("Caterpie", 1, 1), ("Caterpie", 1, 2), ("Caterpie", 1, 4)));
    }
}