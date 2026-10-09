using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Support;

/// <summary>Builders for test Pokémon based on the HANDOFF §6 data.</summary>
public static class Pokemons
{
    public static readonly int[] SquirtleBase = [44, 48, 65, 50, 64, 43];
    public static readonly int[] WartortleBase = [59, 63, 80, 65, 80, 58];
    public static readonly int[] BlastoiseBase = [79, 83, 100, 85, 105, 78];

    public static TrainingPokemon Squirtle(string nature = "Timid", int[]? targets = null) => new()
    {
        Name = "Squirtle",
        Nature = nature,
        Species =
        [
            new SpeciesEntry { Name = "Squirtle", Base = [.. SquirtleBase] },
            new SpeciesEntry { Name = "Wartortle", Base = [.. WartortleBase] },
            new SpeciesEntry { Name = "Blastoise", Base = [.. BlastoiseBase] },
        ],
        Targets = targets ?? [252, 0, 2, 252, 0, 4],
        Order = 1,
    };

    public static TrainingPokemon WithEvents(this TrainingPokemon pokemon, params (string Opponent, int Count, int Multiplier)[] events)
    {
        foreach (var (opponent, count, multiplier) in events)
        {
            pokemon.Events.Add(new BattleEvent { Opponent = opponent, Count = count, Multiplier = multiplier });
        }

        return pokemon;
    }
}