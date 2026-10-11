using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Settings;

namespace EvTrainer.Tests.Components.Settings;

public class SettingsFormTests : AppTestContext
{
    [Fact]
    public void Shows_fields_with_nature_options_numeric_targets_and_the_species_line()
    {
        Seed(Pokemons.Squirtle());

        var cut = Render<SettingsForm>();

        Assert.Empty(cut.FindAll("#settings-name"));
        Assert.Equal(26, cut.FindAll("select#settings-nature option").Count);
        Assert.Equal(6, cut.FindAll(".targets input").Count);
        Assert.All(cut.FindAll(".targets input"), i => Assert.Equal("numeric", i.GetAttribute("inputmode")));
        Assert.Equal(3, cut.FindAll(".species-chip").Count);
    }

    [Fact]
    public void Target_sum_is_live_and_warns_above_510_but_still_saves()
    {
        Seed(Pokemons.Squirtle(targets: [252, 0, 2, 252, 0, 4]));
        var cut = Render<SettingsForm>();

        Assert.Equal(T("Settings.Targets.Sum", 510), cut.Find(".targets-sum").TextContent.Trim());
        cut.FindAll(".targets input")[1].Input("10");

        Assert.Equal(T("Settings.Targets.Sum", 520), cut.Find(".targets-sum").TextContent.Trim());
        Assert.Equal(T("Settings.Targets.OverLimit"), cut.Find(".targets .warn").TextContent.Trim());
        Assert.False(cut.Find("button.save").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData("256")]
    [InlineData("-1")]
    [InlineData("x")]
    public void Out_of_range_targets_disable_save(string value)
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<SettingsForm>();

        cut.FindAll(".targets input")[0].Input(value);

        Assert.True(cut.Find("button.save").HasAttribute("disabled"));
    }

    [Fact]
    public void Saves_without_typing_any_name()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<SettingsForm>();

        Assert.False(cut.Find("button.save").HasAttribute("disabled"));
        cut.Find("button.save").Click();

        Assert.Equal("Squirtle", Repository.Stored["p1"].CurrentSpecies);
    }

    [Fact]
    public void New_pokemon_needs_a_species_and_its_current_form_is_the_one_picked()
    {
        Store.Load();
        Store.AddPokemon();
        var cut = Render<SettingsForm>();
        Assert.True(cut.Find("button.save").HasAttribute("disabled"));

        cut.Find("input.species-search").Input("charm");
        cut.Find("button.species-option[data-name='Charmeleon']").Click();
        cut.Find("button.save").Click();

        var saved = Store.Selected!;
        Assert.Equal("Charmeleon", saved.CurrentSpecies);
        Assert.Equal(["Charmander", "Charmeleon", "Charizard"], saved.Species.Select(s => s.Name));
        Assert.Equal([39, 52, 43, 60, 50, 65], saved.Species[0].Base);
    }

    [Fact]
    public void Saving_updates_the_pokemon()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<SettingsForm>();

        cut.Find("select#settings-nature").Change("Modest");
        cut.FindAll(".targets input")[5].Input("8");
        cut.Find("button.save").Click();

        var saved = Repository.Stored["p1"];
        Assert.Equal("Squirtle", saved.CurrentSpecies);
        Assert.Equal("Modest", saved.Nature);
        Assert.Equal([252, 0, 2, 252, 0, 8], saved.Targets);
        Assert.Equal(["Squirtle", "Wartortle", "Blastoise"], saved.Species.Select(s => s.Name));
        Assert.Equal(T("Settings.Saved"), Toasts.Message);
    }

    [Fact]
    public void Tapping_a_species_of_the_line_makes_it_the_current_form()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<SettingsForm>();

        cut.Find("li.species-chip:nth-child(2) button.species-current").Click();
        cut.Find("button.save").Click();

        Assert.Equal("Wartortle", Repository.Stored["p1"].CurrentSpecies);
    }

    [Fact]
    public void Tapping_an_evolution_makes_it_the_current_form()
    {
        var eevee = Pokemons.Squirtle();
        eevee.CurrentSpecies = "Eevee";
        eevee.Species = [new() { Name = "Eevee", Base = [55, 55, 50, 45, 65, 55] }];
        Seed(eevee);
        var cut = Render<SettingsForm>();

        cut.Find("button.evolution-option[data-name='Vaporeon']").Click();
        cut.Find("button.save").Click();

        var saved = Repository.Stored["p1"];
        Assert.Equal("Vaporeon", saved.CurrentSpecies);
        Assert.Equal(["Eevee", "Vaporeon"], saved.Species.Select(s => s.Name));
    }

    [Fact]
    public void Changing_to_a_species_outside_the_line_makes_it_the_current_form()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<SettingsForm>();

        cut.Find("button.change-species").Click();
        cut.Find("input.species-search").Input("bulba");
        cut.Find("button.species-option[data-name='Bulbasaur']").Click();
        cut.Find("button.save").Click();

        var saved = Repository.Stored["p1"];
        Assert.Equal("Bulbasaur", saved.CurrentSpecies);
        Assert.Equal(["Bulbasaur", "Ivysaur", "Venusaur"], saved.Species.Select(s => s.Name));
    }
}