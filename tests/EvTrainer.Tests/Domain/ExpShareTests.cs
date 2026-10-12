using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 012: the Exp. Share holder gets the whole EV yield, doubled only by its own Pokérus.</summary>
public class ExpShareTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 2)]
    [InlineData(false, true, 1)]
    [InlineData(true, true, 2)]
    public void Holder_gets_the_whole_yield_doubled_only_by_its_own_pokerus(bool pokerus, bool macho, int expected)
    {
        var holder = Pokemons.Squirtle();
        holder.Pokerus = pokerus;
        holder.MachoBrace = macho;

        var result = BattleLog.RecordShared(holder, "Caterpie", Data);

        Assert.Equal([expected, 0, 0, 0, 0, 0], result.Gain);
        var e = Assert.Single(holder.Events);
        Assert.Equal(("Caterpie", 1, expected, true), (e.Opponent, e.Count, e.Multiplier, e.Shared));
    }

    [Fact]
    public void Same_shared_battles_group_but_never_with_own_battles_or_adjustments()
    {
        var holder = Pokemons.Squirtle().WithEvents(("Caterpie", 1, 1));

        BattleLog.RecordShared(holder, "Caterpie", Data);
        BattleLog.RecordShared(holder, "Caterpie", Data);

        Assert.Equal([(1, false), (2, true)], holder.Events.Select(e => (e.Count, e.Shared)));

        holder.Pokerus = true;
        BattleLog.RecordShared(holder, "Caterpie", Data);
        holder.Events.Add(new BattleEvent { Opponent = "", SetTotals = [0, 0, 0, 0, 0, 0] });
        BattleLog.RecordShared(holder, "Caterpie", Data);

        Assert.Equal(5, holder.Events.Count);
        Assert.Equal((1, 2, true), (holder.Events[2].Count, holder.Events[2].Multiplier, holder.Events[2].Shared));
        Assert.True(holder.Events[4].Shared);
    }

    [Fact]
    public void Holder_respects_its_own_caps()
    {
        var full = Pokemons.Squirtle();
        full.Events.Add(new BattleEvent { Opponent = "", SetTotals = [255, 255, 0, 0, 0, 0] });

        var result = BattleLog.RecordShared(full, "Caterpie", Data);

        Assert.False(result.GainedAny);
    }

    [Fact]
    public void Own_battles_are_not_marked_shared_nor_grouped_with_shared_ones()
    {
        var pokemon = Pokemons.Squirtle();
        BattleLog.RecordShared(pokemon, "Caterpie", Data);

        BattleLog.Record(pokemon, "Caterpie", Data);

        Assert.Equal([true, false], pokemon.Events.Select(e => e.Shared));
    }

    [Fact]
    public void Undo_removes_one_unit_of_the_last_shared_battle_of_that_opponent()
    {
        var holder = Pokemons.Squirtle();
        BattleLog.RecordShared(holder, "Caterpie", Data);
        BattleLog.RecordShared(holder, "Caterpie", Data);

        BattleLog.UndoLastShared(holder, "Caterpie", Data);
        Assert.Equal(1, Assert.Single(holder.Events).Count);

        BattleLog.UndoLastShared(holder, "Caterpie", Data);
        Assert.Empty(holder.Events);
    }

    [Fact]
    public void Undo_does_nothing_when_the_last_battle_is_not_that_shared_one()
    {
        var holder = Pokemons.Squirtle();
        BattleLog.RecordShared(holder, "Caterpie", Data);
        BattleLog.Record(holder, "Caterpie", Data);

        BattleLog.UndoLastShared(holder, "Caterpie", Data);
        BattleLog.UndoLastShared(holder, "Weedle", Data);

        Assert.Equal([(1, true), (1, false)], holder.Events.Select(e => (e.Count, e.Shared)));
    }
}