using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class ManualAdjustmentTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    private static BattleEvent Adjust(params int[] totals) => new() { Opponent = "", SetTotals = totals };

    private static TrainingPokemon With(params BattleEvent[] events)
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.Events.AddRange(events);
        return pokemon;
    }

    private static BattleEvent Battle(string opponent, int count = 1, int multiplier = 1) =>
        new() { Opponent = opponent, Count = count, Multiplier = multiplier };

    [Fact]
    public void Adjustment_sets_the_totals_at_that_point()
    {
        var pokemon = With(Battle("Caterpie", 10), Adjust(12, 0, 2, 26, 0, 4));

        Assert.Equal([12, 0, 2, 26, 0, 4], Evs.Totals(pokemon.Events, Data));
    }

    [Fact]
    public void Later_battles_add_on_top_of_the_adjustment()
    {
        var pokemon = With(Battle("Caterpie", 10), Adjust(12, 0, 2, 26, 0, 4), Battle("Caterpie"));

        Assert.Equal([13, 0, 2, 26, 0, 4], Evs.Totals(pokemon.Events, Data));
    }

    [Fact]
    public void Vitamin_rule_applies_after_the_adjustment()
    {
        Assert.Equal([0, 0, 0, 100, 0, 0], Evs.Totals(With(Adjust(0, 0, 0, 95, 0, 0), Battle("Calcium")).Events, Data));
        Assert.Equal([0, 0, 0, 100, 0, 0], Evs.Totals(With(Adjust(0, 0, 0, 100, 0, 0), Battle("Calcium")).Events, Data));
    }

    [Fact]
    public void Total_cap_applies_after_the_adjustment()
    {
        var pokemon = With(Adjust(255, 255, 0, 0, 0, 0), Battle("Abra"));

        Assert.Equal([255, 255, 0, 0, 0, 0], Evs.Totals(pokemon.Events, Data));
    }

    [Fact]
    public void The_last_adjustment_wins()
    {
        var pokemon = With(Adjust(1, 1, 1, 1, 1, 1), Battle("Caterpie"), Adjust(5, 0, 0, 0, 0, 0));

        Assert.Equal([5, 0, 0, 0, 0, 0], Evs.Totals(pokemon.Events, Data));
    }

    [Fact]
    public void Removing_battles_before_the_adjustment_does_not_change_totals()
    {
        var pokemon = With(Battle("Caterpie", 10), Battle("Abra", 3), Adjust(12, 0, 2, 26, 0, 4));
        pokemon.Events.RemoveAt(0);

        Assert.Equal([12, 0, 2, 26, 0, 4], Evs.Totals(pokemon.Events, Data));
    }

    [Fact]
    public void Counts_and_recents_ignore_adjustments()
    {
        var pokemon = With(Battle("Caterpie", 2), Adjust(0, 0, 0, 0, 0, 0), Battle("Caterpie"));

        Assert.Equal(3, Evs.CountFor(pokemon.Events, "Caterpie", Data));
        Assert.Equal(3, Evs.CountsByOpponent(pokemon.Events, Data)["Caterpie"]);
        Assert.Equal(["Caterpie"], Evs.Recent(pokemon.Events, Data).Select(o => o.Name));
    }

    [Fact]
    public void Recording_after_an_adjustment_never_groups_into_it()
    {
        var pokemon = With(Adjust(0, 0, 0, 0, 0, 0));

        BattleLog.Record(pokemon, "Caterpie", Data);

        Assert.Equal(2, pokemon.Events.Count);
        Assert.True(pokemon.Events[0].IsManualAdjustment);
        Assert.Equal(("Caterpie", 1), (pokemon.Events[1].Opponent, pokemon.Events[1].Count));
    }
}