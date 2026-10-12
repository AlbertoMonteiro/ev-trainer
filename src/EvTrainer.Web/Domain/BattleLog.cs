using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>Actual EV gain per stat of one recorded battle (all zeros when the caps were reached).</summary>
public sealed record RecordResult(int[] Gain)
{
    public bool GainedAny => Gain.Any(g => g > 0);
}

/// <summary>
/// One recorded battle (spec 012): the selected Pokémon's gain and, when the Exp. Share is with another Pokémon,
/// the holder's gain.
/// </summary>
public sealed record SharedRecord(string MainId, string Opponent, RecordResult Main, string? HolderId, RecordResult? Holder);

/// <summary>Records and undoes battles (HANDOFF §4.2).</summary>
public static class BattleLog
{
    /// <summary>
    /// Adds one battle against <paramref name="opponent"/> with the current multiplier, grouping it into the
    /// last event when it has the same canonical opponent and multiplier (never into a manual adjustment or a
    /// battle received through the Exp. Share).
    /// </summary>
    public static RecordResult Record(TrainingPokemon pokemon, string opponent, ReferenceData data) =>
        Add(pokemon, opponent, Evs.Multiplier(pokemon.Pokerus, pokemon.MachoBrace), shared: false, data);

    /// <summary>
    /// Adds the battle the Exp. Share holder receives (spec 012): the whole EV yield, doubled only by the holder's
    /// own Pokérus — Macho Brace never counts, the holder is holding the Exp. Share.
    /// </summary>
    public static RecordResult RecordShared(TrainingPokemon holder, string opponent, ReferenceData data) =>
        Add(holder, opponent, Evs.Multiplier(holder.Pokerus, machoBrace: false), shared: true, data);

    private static RecordResult Add(TrainingPokemon pokemon, string opponent, int multiplier, bool shared, ReferenceData data)
    {
        var before = Evs.Totals(pokemon.Events, data);
        var name = data.Canon(opponent);

        if (pokemon.Events.Count > 0
            && pokemon.Events[^1] is { IsManualAdjustment: false } last
            && last.Shared == shared
            && data.Canon(last.Opponent) == name
            && last.Multiplier == multiplier)
        {
            last.Count++;
        }
        else
        {
            pokemon.Events.Add(new BattleEvent { Opponent = name, Count = 1, Multiplier = multiplier, Shared = shared });
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

    /// <summary>
    /// Undoes one unit of the holder's last battle when it is the shared battle against <paramref name="opponent"/>;
    /// otherwise (the holder battled since, or it is another opponent) does nothing.
    /// </summary>
    public static void UndoLastShared(TrainingPokemon holder, string opponent, ReferenceData data)
    {
        if (holder.Events.Count > 0
            && holder.Events[^1] is { Shared: true, IsManualAdjustment: false } last
            && data.Canon(last.Opponent) == data.Canon(opponent))
        {
            UndoLast(holder);
        }
    }
}