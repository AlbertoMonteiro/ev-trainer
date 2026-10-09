using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class BattleLogTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 2)]
    [InlineData(false, true, 2)]
    [InlineData(true, true, 4)]
    public void Multiplier_combines_pokerus_and_macho_brace(bool pokerus, bool macho, int expected) =>
        Assert.Equal(expected, Evs.Multiplier(pokerus, macho));

    [Fact]
    public void Consecutive_taps_with_same_multiplier_are_grouped()
    {
        var pokemon = Pokemons.Squirtle();

        BattleLog.Record(pokemon, "Caterpie", Data);
        BattleLog.Record(pokemon, "Caterpie", Data);

        var single = Assert.Single(pokemon.Events);
        Assert.Equal(("Caterpie", 2, 1), (single.Opponent, single.Count, single.Multiplier));
    }

    [Fact]
    public void Alias_and_canonical_names_group_together()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Bulbasaur (rival)", 1, 1));

        BattleLog.Record(pokemon, "Bulbasaur", Data);

        Assert.Equal(2, Assert.Single(pokemon.Events).Count);
    }

    [Fact]
    public void Different_multiplier_starts_a_new_event()
    {
        var pokemon = Pokemons.Squirtle();
        BattleLog.Record(pokemon, "Caterpie", Data);
        pokemon.Pokerus = true;

        BattleLog.Record(pokemon, "Caterpie", Data);

        Assert.Equal([(1, 1), (1, 2)], pokemon.Events.Select(e => (e.Count, e.Multiplier)));
    }

    [Fact]
    public void Record_returns_the_actual_gain()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.Pokerus = true;
        pokemon.MachoBrace = true;

        var result = BattleLog.Record(pokemon, "Caterpie", Data);

        Assert.Equal([4, 0, 0, 0, 0, 0], result.Gain);
        Assert.True(result.GainedAny);
    }

    [Fact]
    public void Record_at_the_cap_gains_nothing_but_is_recorded()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 255, 1));

        var result = BattleLog.Record(pokemon, "Caterpie", Data);

        Assert.Equal(new int[6], result.Gain);
        Assert.False(result.GainedAny);
        Assert.Equal(256, pokemon.Events[0].Count);
    }

    [Fact]
    public void Undo_decrements_and_removes_when_zero()
    {
        var pokemon = Pokemons.Squirtle();
        BattleLog.Record(pokemon, "Caterpie", Data);
        BattleLog.Record(pokemon, "Caterpie", Data);

        BattleLog.UndoLast(pokemon);
        Assert.Equal(1, pokemon.Events[0].Count);

        BattleLog.UndoLast(pokemon);
        Assert.Empty(pokemon.Events);

        BattleLog.UndoLast(pokemon);
        Assert.Empty(pokemon.Events);
    }

    [Fact]
    public void Toggling_pokerus_later_does_not_rewrite_the_past()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.Pokerus = true;
        BattleLog.Record(pokemon, "Caterpie", Data);

        pokemon.Pokerus = false;

        Assert.Equal([2, 0, 0, 0, 0, 0], Evs.Totals(pokemon.Events, Data));
    }

    [Fact]
    public void CountFor_sums_by_canonical_name()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Nidoran F", 2, 1), ("Caterpie", 3, 1), ("Nidoran♀", 1, 2));

        Assert.Equal(3, Evs.CountFor(pokemon.Events, "Nidoran♀", Data));
        Assert.Equal(3, Evs.CountFor(pokemon.Events, "Caterpie", Data));
        Assert.Equal(0, Evs.CountFor(pokemon.Events, "Pidgey", Data));
    }
}