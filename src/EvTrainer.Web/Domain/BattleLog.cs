using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>Actual EV gain per stat of one recorded battle (all zeros when the caps were reached).</summary>
public sealed record RecordResult(int[] Gain)
{
    public bool GainedAny => Gain.Any(g => g > 0);
}

/// <summary>Records and undoes battles (HANDOFF §4.2).</summary>
public static class BattleLog
{
    /// <summary>
    /// Adds one battle against <paramref name="opponent"/> with the current multiplier, grouping it into the
    /// last event when it has the same canonical opponent and multiplier.
    /// </summary>
    public static RecordResult Record(TrainingPokemon pokemon, string opponent, ReferenceData data)
    {
        var before = Evs.Totals(pokemon.Events, data);
        var multiplier = Evs.Multiplier(pokemon.Pokerus, pokemon.MachoBrace);
        var name = data.Canon(opponent);

        if (pokemon.Events.Count > 0
            && pokemon.Events[^1] is var last
            && data.Canon(last.Opponent) == name
            && last.Multiplier == multiplier)
        {
            last.Count++;
        }
        else
        {
            pokemon.Events.Add(new BattleEvent { Opponent = name, Count = 1, Multiplier = multiplier });
        }

        var after = Evs.Totals(pokemon.Events, data);
        return new RecordResult([.. after.Select((value, i) => value - before[i])]);
    }

    /// <summary>Removes one unit from the last event, dropping it when it reaches zero.</summary>
    public static void UndoLast(TrainingPokemon pokemon)
    {
        if (pokemon.Events.Count == 0)
        {
            return;
        }

        var last = pokemon.Events[^1];
        if (--last.Count <= 0)
        {
            pokemon.Events.RemoveAt(pokemon.Events.Count - 1);
        }
    }
}