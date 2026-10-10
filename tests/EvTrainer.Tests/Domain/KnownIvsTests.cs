using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class KnownIvsTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    private static LevelEntry Level(int lv, string sp, int[] st, int[] ev) => new() { Level = lv, Species = sp, Stats = st, Evs = ev };

    private static TrainingPokemon WithHandoffLevels()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        pokemon.IvLog.AddRange(
        [
            Level(5, "Squirtle", [20, 9, 12, 11, 12, 11], [0, 0, 0, 0, 0, 0]),
            Level(13, "Squirtle", [37, 18, 24, 21, 25, 22], [8, 0, 2, 1, 0, 3]),
            Level(16, "Wartortle", [49, 25, 33, 30, 35, 30], [21, 0, 2, 5, 0, 3]),
            Level(18, "Wartortle", [54, 27, 37, 33, 39, 34], [26, 0, 2, 13, 0, 3]),
            Level(20, "Wartortle", [60, 30, 40, 37, 42, 37], [36, 0, 2, 21, 0, 4]),
            Level(21, "Wartortle", [62, 31, 42, 39, 44, 38], [38, 0, 2, 27, 0, 4]),
        ]);
        return pokemon;
    }

    [Fact]
    public void Known_value_matching_the_notes_wins()
    {
        var pokemon = WithHandoffLevels();
        pokemon.KnownIvs[5] = 30;
        pokemon.KnownIvs[0] = 24;

        var final = Ivs.Final(pokemon, Data);

        Assert.Equal(new FinalKnown(30, MatchesNotes: true), final[5]);
        Assert.Equal(new FinalKnown(24, MatchesNotes: true), final[0]);
    }

    [Fact]
    public void Known_value_outside_the_notes_is_flagged()
    {
        var pokemon = WithHandoffLevels();
        pokemon.KnownIvs[5] = 10;

        Assert.Equal(new FinalKnown(10, MatchesNotes: false), Ivs.Final(pokemon, Data)[5]);
    }

    [Fact]
    public void Known_value_without_notes_matches()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        pokemon.KnownIvs[0] = 5;

        var final = Ivs.Final(pokemon, Data);

        Assert.Equal(new FinalKnown(5, MatchesNotes: true), final[0]);
        Assert.IsType<FinalNoData>(final[1]);
    }

    [Fact]
    public void Known_value_over_a_conflict_does_not_match()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        pokemon.IvLog.Add(Level(5, "Squirtle", [20, 9, 12, 11, 12, 11], new int[6]));
        pokemon.IvLog.Add(Level(5, "Squirtle", [19, 9, 12, 11, 12, 11], new int[6]));
        Assert.IsType<FinalConflict>(Ivs.Final(pokemon, Data)[0]);

        pokemon.KnownIvs[0] = 20;

        Assert.Equal(new FinalKnown(20, MatchesNotes: false), Ivs.Final(pokemon, Data)[0]);
    }

    [Fact]
    public void Stats_without_known_values_are_unchanged()
    {
        var pokemon = WithHandoffLevels();
        pokemon.KnownIvs[5] = 30;

        Assert.Equal(["23-25", "20-21", "18-19", "26-28", "29", "30*"], Ivs.Final(pokemon, Data).Select(IvTests.Show));
    }
}