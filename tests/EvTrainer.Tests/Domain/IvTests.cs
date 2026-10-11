using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class IvTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    internal static string Show(IvCell cell) => cell switch
    {
        IvSpan(var min, var max) => min == max ? $"{min}" : $"{min}-{max}",
        IvInconsistent => "?",
        IvNoData => "·",
        IvUnknowable => "n/a",
    };

    internal static string Show(FinalCell cell) => cell switch
    {
        FinalSpan(var low, var high) => $"{low}-{high}",
        FinalSingle(var value) => $"{value}",
        FinalConflict => "conferir",
        FinalNoData => "—",
        FinalKnown(var value, var matches) => matches ? $"{value}*" : $"{value}!",
    };

    private static LevelEntry Level(int lv, string sp, int[] st, int[] ev) => new() { Level = lv, Species = sp, Stats = st, Evs = ev };

    [Fact]
    public void Squirtle_level_5_ranges()
    {
        var pokemon = Pokemons.Squirtle("Timid");

        var cells = Ivs.RangeFor(Level(5, "Squirtle", [20, 9, 12, 11, 12, 11], new int[6]), pokemon, Data);

        Assert.Equal(["12-31", "4-31", "10-29", "20-31", "12-31", "14-31"], cells.Select(Show));
    }

    [Fact]
    public void Intersection_of_the_six_handoff_levels()
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

        var final = Ivs.Final(pokemon, Data);

        Assert.Equal(["23-25", "20-21", "18-19", "26-28", "29", "30"], final.Select(Show));
    }

    [Fact]
    public void Impossible_stat_is_inconsistent()
    {
        var cells = Ivs.RangeFor(Level(5, "Squirtle", [99, 9, 12, 11, 12, 11], new int[6]), Pokemons.Squirtle("Timid"), Data);

        Assert.Equal("?", Show(cells[0]));
        Assert.Equal("4-31", Show(cells[1]));
    }

    [Fact]
    public void Unknown_species_is_inconsistent_everywhere()
    {
        var cells = Ivs.RangeFor(Level(5, "Charmander", [20, 9, 12, 11, 12, 11], new int[6]), Pokemons.Squirtle(), Data);

        Assert.All(cells, c => Assert.IsType<IvInconsistent>(c));
    }

    [Fact]
    public void Zero_stat_means_no_data()
    {
        var cells = Ivs.RangeFor(Level(5, "Squirtle", [20, 0, 12, 11, 12, 11], new int[6]), Pokemons.Squirtle("Timid"), Data);

        Assert.IsType<IvNoData>(cells[1]);
    }

    [Fact]
    public void Disjoint_ranges_ask_to_check_and_no_rows_give_no_data()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        Assert.All(Ivs.Final(pokemon, Data), c => Assert.IsType<FinalNoData>(c));

        pokemon.IvLog.Add(Level(5, "Squirtle", [20, 9, 12, 11, 12, 11], new int[6]));
        pokemon.IvLog.Add(Level(5, "Squirtle", [19, 9, 12, 11, 12, 11], new int[6]));

        Assert.IsType<FinalConflict>(Ivs.Final(pokemon, Data)[0]);
    }

    [Fact]
    public void Empty_nature_is_neutral()
    {
        var neutral = Ivs.RangeFor(Level(5, "Squirtle", [20, 9, 12, 11, 12, 11], new int[6]), Pokemons.Squirtle(""), Data);
        var hardy = Ivs.RangeFor(Level(5, "Squirtle", [20, 9, 12, 11, 12, 11], new int[6]), Pokemons.Squirtle("Hardy"), Data);

        Assert.Equal(hardy.Select(Show), neutral.Select(Show));
    }

    [Fact]
    public void Pure_overload_matches_the_level_entry_version()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        LevelEntry[] levels =
        [
            Level(5, "Squirtle", [20, 9, 12, 11, 12, 11], [0, 0, 0, 0, 0, 0]),
            Level(13, "Squirtle", [37, 18, 24, 21, 25, 22], [8, 0, 2, 1, 0, 3]),
            Level(16, "Wartortle", [49, 25, 33, 30, 35, 30], [21, 0, 2, 5, 0, 3]),
            Level(18, "Wartortle", [54, 27, 37, 33, 39, 34], [26, 0, 2, 13, 0, 3]),
            Level(20, "Wartortle", [60, 30, 40, 37, 42, 37], [36, 0, 2, 21, 0, 4]),
            Level(21, "Wartortle", [62, 31, 42, 39, 44, 38], [38, 0, 2, 27, 0, 4]),
        ];

        foreach (var level in levels)
        {
            var species = pokemon.Species.First(s => s.Name == level.Species);
            Assert.Equal(
                Ivs.RangeFor(level, pokemon, Data).Select(Show),
                Ivs.RangeFor(species, Data.FindNature("Timid"), level.Level, level.Stats, level.Evs).Select(Show));
        }
    }

    [Fact]
    public void Pure_overload_without_species_is_inconsistent()
    {
        var cells = Ivs.RangeFor(null, Data.FindNature("Timid"), 5, [20, 9, 12, 11, 12, 0], new int[6]);

        Assert.All(cells[..5], c => Assert.IsType<IvInconsistent>(c));
        Assert.IsType<IvNoData>(cells[5]);
    }

    [Fact]
    public void Shedinja_hp_cannot_be_known_and_is_left_out_of_the_final_row()
    {
        var shedinja = new SpeciesEntry { Name = "Shedinja", Base = [1, 90, 45, 30, 30, 40] };

        var cells = Ivs.RangeFor(shedinja, Data.FindNature("Hardy"), 25, [1, 54, 33, 26, 26, 30], new int[6]);

        Assert.IsType<IvUnknowable>(cells[0]);
        Assert.IsType<IvSpan>(cells[1]);

        var pokemon = new TrainingPokemon { Nature = "Hardy", Species = [shedinja] };
        pokemon.IvLog.Add(Level(25, "Shedinja", [1, 54, 33, 26, 26, 30], new int[6]));
        Assert.IsType<FinalNoData>(Ivs.Final(pokemon, Data)[0]);
    }
}