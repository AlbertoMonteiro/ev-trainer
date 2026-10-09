using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Ivs;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Components.Ivs;

public class LevelFormTests : AppTestContext
{
    [Fact]
    public void Prefills_current_evs_and_last_used_species_with_numeric_inputs()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 3, 1), ("Abra", 2, 1));
        pokemon.IvLog.Add(new LevelEntry { Level = 16, Species = "Wartortle", Stats = [49, 25, 33, 30, 35, 30], Evs = new int[6] });
        Seed(pokemon);

        var cut = Render<LevelForm>();

        Assert.Equal("Wartortle", cut.Find("select#level-species").GetAttribute("value"));
        Assert.Equal(["Squirtle", "Wartortle", "Blastoise"], cut.FindAll("select#level-species option").Select(o => o.GetAttribute("value")));
        Assert.Equal(["3", "0", "0", "2", "0", "0"], cut.FindAll(".evs input").Select(i => i.GetAttribute("value")));
        Assert.All(cut.FindAll("input"), i => Assert.Equal("numeric", i.GetAttribute("inputmode")));
    }

    [Fact]
    public void Save_is_disabled_until_level_and_stats_are_filled()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<LevelForm>();

        Assert.True(cut.Find("button.save-level").HasAttribute("disabled"));

        cut.Find("#level-number").Input("21");
        var stats = cut.FindAll(".stats input");
        int[] values = [62, 31, 42, 39, 44];
        for (var i = 0; i < values.Length; i++)
        {
            cut.FindAll(".stats input")[i].Input(values[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        Assert.True(cut.Find("button.save-level").HasAttribute("disabled"));
        cut.FindAll(".stats input")[5].Input("38");
        Assert.False(cut.Find("button.save-level").HasAttribute("disabled"));
    }

    [Fact]
    public void Saving_inserts_sorted_by_level()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.IvLog.Add(new LevelEntry { Level = 30, Species = "Wartortle", Stats = [1, 1, 1, 1, 1, 1], Evs = new int[6] });
        Seed(pokemon);
        var cut = Render<LevelForm>();

        cut.Find("#level-number").Input("21");
        cut.Find("select#level-species").Change("Wartortle");
        int[] values = [62, 31, 42, 39, 44, 38];
        for (var i = 0; i < 6; i++)
        {
            cut.FindAll(".stats input")[i].Input(values[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        cut.Find("button.save-level").Click();

        Assert.Equal([21, 30], Repository.Stored["p1"].IvLog.Select(l => l.Level));
        Assert.Equal([62, 31, 42, 39, 44, 38], Repository.Stored["p1"].IvLog[0].Stats);
        Assert.Equal(T("Ivs.Saved", 21), Toasts.Message);
    }
}