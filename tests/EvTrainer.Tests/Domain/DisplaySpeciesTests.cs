using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Domain;

public class DisplaySpeciesTests
{
    [Fact]
    public void Uses_the_species_of_the_latest_level_note()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [1, 1, 1, 1, 1, 1], Evs = new int[6] });
        pokemon.IvLog.Add(new LevelEntry { Level = 16, Species = "Wartortle", Stats = [1, 1, 1, 1, 1, 1], Evs = new int[6] });

        Assert.Equal("Wartortle", pokemon.DisplaySpecies);
    }

    [Fact]
    public void Ignores_a_note_whose_species_is_not_in_the_line()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.IvLog.Add(new LevelEntry { Level = 16, Species = "Charmander", Stats = [1, 1, 1, 1, 1, 1], Evs = new int[6] });

        Assert.Equal("Squirtle", pokemon.DisplaySpecies);
    }

    [Fact]
    public void Falls_back_to_the_first_species_then_to_the_name()
    {
        Assert.Equal("Squirtle", Pokemons.Squirtle().DisplaySpecies);
        Assert.Equal("Shelly", new TrainingPokemon { Name = "Shelly" }.DisplaySpecies);
    }
}