using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Domain;

public class CurrentFormTests
{
    private static LevelEntry Note(int level, string species) =>
        new() { Level = level, Species = species, Stats = [1, 1, 1, 1, 1, 1], Evs = new int[6] };

    [Fact]
    public void Advances_to_a_later_species_of_the_line()
    {
        var pokemon = Pokemons.Squirtle();

        pokemon.AdvanceTo("Wartortle");

        Assert.Equal("Wartortle", pokemon.CurrentSpecies);
    }

    [Fact]
    public void Never_goes_back_to_an_earlier_species()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.CurrentSpecies = "Wartortle";

        pokemon.AdvanceTo("Squirtle");

        Assert.Equal("Wartortle", pokemon.CurrentSpecies);
    }

    [Fact]
    public void Ignores_a_species_outside_the_line()
    {
        var pokemon = Pokemons.Squirtle();

        pokemon.AdvanceTo("Charmander");

        Assert.Equal("Squirtle", pokemon.CurrentSpecies);
    }

    [Fact]
    public void An_empty_current_species_takes_any_species_of_the_line()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.CurrentSpecies = "";

        pokemon.AdvanceTo("Squirtle");

        Assert.Equal("Squirtle", pokemon.CurrentSpecies);
    }

    [Fact]
    public void Inserting_a_level_note_keeps_the_log_sorted_and_advances()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.InsertLevel(Note(20, "Squirtle"));

        pokemon.InsertLevel(Note(16, "Wartortle"));

        Assert.Equal([16, 20], pokemon.IvLog.Select(l => l.Level));
        Assert.Equal("Wartortle", pokemon.CurrentSpecies);
    }

    [Fact]
    public void A_late_note_of_an_earlier_species_does_not_devolve()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.CurrentSpecies = "Blastoise";

        pokemon.InsertLevel(Note(5, "Squirtle"));

        Assert.Equal("Blastoise", pokemon.CurrentSpecies);
    }

    private static readonly List<SpeciesEntry> RaltsLine =
    [
        new() { Name = "Ralts", Base = [28, 25, 25, 45, 35, 40] },
        new() { Name = "Kirlia", Base = [38, 35, 35, 65, 55, 50] },
        new() { Name = "Gardevoir", Base = [68, 65, 65, 125, 115, 80] },
    ];

    [Theory]
    [InlineData("Kirlia", "", "Kirlia")]
    [InlineData("kirlia", "", "Kirlia")]
    [InlineData(" Kirlia ", "", "Kirlia")]
    [InlineData("Gardi", "5:Ralts 20:Kirlia", "Kirlia")]
    [InlineData("Gardi", "20:Kirlia 30:Ralts", "Kirlia")]
    [InlineData("Gardi", "", "Ralts")]
    [InlineData(null, "", "Ralts")]
    [InlineData("Gardi", "20:Charmander", "Ralts")]
    public void Resolve_deduces_the_current_species_of_old_data(string? name, string notes, string expected)
    {
        List<LevelEntry> ivLog = [.. notes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Split(':'))
            .Select(n => Note(int.Parse(n[0], System.Globalization.CultureInfo.InvariantCulture), n[1]))];

        Assert.Equal(expected, CurrentForm.Resolve(name, RaltsLine, ivLog));
    }

    [Fact]
    public void Resolve_gives_nothing_when_the_line_has_no_named_species()
    {
        Assert.Equal("", CurrentForm.Resolve("X", [new SpeciesEntry()], []));
    }

    [Fact]
    public void Clone_copies_the_current_species()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.CurrentSpecies = "Wartortle";

        Assert.Equal("Wartortle", pokemon.Clone().CurrentSpecies);
    }
}