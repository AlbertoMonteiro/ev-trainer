using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Ivs;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Components.Ivs;

public class KnownIvsPanelTests : AppTestContext
{
    private static TrainingPokemon WithLevel5()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        pokemon.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 11], Evs = new int[6] });
        return pokemon;
    }

    [Fact]
    public void Collapsible_section_with_six_empty_fields()
    {
        Seed(WithLevel5());

        var cut = Render<KnownIvsPanel>();

        Assert.Equal(T("KnownIvs.Title"), cut.Find("details.known-ivs summary").TextContent.Trim());
        Assert.Equal(T("KnownIvs.Hint"), cut.Find("details.known-ivs .hint").TextContent.Trim());
        var inputs = cut.FindAll(".known-ivs .six input");
        Assert.Equal(6, inputs.Count);
        Assert.All(inputs, i => Assert.Equal("", i.GetAttribute("value") ?? ""));
        Assert.All(inputs, i => Assert.Equal("numeric", i.GetAttribute("inputmode")));
    }

    [Theory]
    [InlineData("32")]
    [InlineData("-1")]
    [InlineData("x")]
    public void Out_of_range_disables_save(string value)
    {
        Seed(WithLevel5());
        var cut = Render<KnownIvsPanel>();

        cut.FindAll(".known-ivs .six input")[5].Input(value);

        Assert.True(cut.Find(".known-ivs button.save").HasAttribute("disabled"));
    }

    [Fact]
    public void Saving_stores_values_with_undo()
    {
        Seed(WithLevel5());
        var cut = Render<KnownIvsPanel>();

        cut.FindAll(".known-ivs .six input")[5].Input("30");
        cut.Find(".known-ivs button.save").Click();

        Assert.Equal([null, null, null, null, null, 30], Repository.Stored["p1"].KnownIvs);
        Assert.Equal(T("KnownIvs.Saved"), Toasts.Message);

        cut.InvokeAsync(Toasts.RunUndo);

        Assert.Equal(new int?[6], Repository.Stored["p1"].KnownIvs);
    }

    [Fact]
    public void Prefills_saved_values_and_clearing_a_field_returns_to_the_estimate()
    {
        var pokemon = WithLevel5();
        pokemon.KnownIvs[5] = 30;
        Seed(pokemon);
        var cut = Render<KnownIvsPanel>();

        Assert.Equal("30", cut.FindAll(".known-ivs .six input")[5].GetAttribute("value"));

        cut.FindAll(".known-ivs .six input")[5].Input("");
        cut.Find(".known-ivs button.save").Click();

        Assert.Equal(new int?[6], Repository.Stored["p1"].KnownIvs);
    }

    [Fact]
    public void Table_shows_known_value_and_flags_a_mismatch()
    {
        var pokemon = WithLevel5();
        pokemon.KnownIvs[5] = 30;
        Seed(pokemon);
        var cut = Render<IvTable>();

        var speed = cut.Find("tr.final").QuerySelectorAll("td.iv")[5];
        Assert.Contains("known", speed.ClassList);
        Assert.DoesNotContain("bad", speed.ClassList);
        Assert.Equal("30", speed.QuerySelector(".value")!.TextContent.Trim());
        Assert.Equal(T("Ivs.Final.Known"), speed.QuerySelector(".known-mark")!.TextContent.Trim());
        Assert.Equal(T("Ivs.Final.KnownNote"), cut.Find(".known-note").TextContent.Trim());
        Assert.Empty(cut.FindAll(".known-mismatch"));

        Store.SetKnownIvs([null, null, null, null, null, 10]);

        speed = cut.Find("tr.final").QuerySelectorAll("td.iv")[5];
        Assert.Contains("bad", speed.ClassList);
        Assert.Equal(T("Ivs.Final.KnownMismatch", T("Stat.Short.5")), cut.Find(".known-mismatch").TextContent.Trim());
    }
}