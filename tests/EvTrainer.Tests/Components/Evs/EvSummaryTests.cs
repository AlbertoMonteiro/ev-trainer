using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Evs;

namespace EvTrainer.Tests.Components.Evs;

public class EvSummaryTests : AppTestContext
{
    [Fact]
    public void Shows_the_sprite_as_title_with_totals_and_six_mini_cards()
    {
        Seed(Pokemons.Squirtle(targets: [252, 0, 2, 252, 0, 4]).WithEvents(("Caterpie", 10, 1), ("Geodude", 2, 1)));

        var cut = Render<EvSummary>();

        var title = cut.Find(".card-head h2.summary-who");
        var image = title.QuerySelector("span[role=img]")!;
        Assert.Equal(T("Evs.Summary.Label", "Squirtle"), image.GetAttribute("aria-label"));
        Assert.Contains("--sprite-size: 64px", image.QuerySelector(".sprite")!.GetAttribute("style"));
        Assert.Equal("sprites/7.png", image.QuerySelector(".sprite img")!.GetAttribute("src"));
        Assert.DoesNotContain("Squirtle", title.TextContent);
        var totals = cut.Find(".card-head .totals").TextContent.Trim();
        Assert.Equal("12 / 510", totals);
        Assert.Equal(T("Evs.Summary.Totals", 12), totals);
        Assert.DoesNotContain("·", totals);
        var minis = cut.FindAll(".mini");
        Assert.Equal(6, minis.Count);
        Assert.Equal(T("Stat.Short.0"), minis[0].QuerySelector(".label")!.TextContent.Trim());
        Assert.Equal("10/252", minis[0].QuerySelector(".value")!.TextContent.Trim());
        Assert.Equal("2/2", minis[2].QuerySelector(".value")!.TextContent.Trim());
    }

    [Fact]
    public void The_sprite_follows_the_current_species()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<EvSummary>();

        Store.SaveSettings("Wartortle", "Timid", [252, 0, 2, 252, 0, 4], Store.Selected!.Species);

        cut.WaitForAssertion(() =>
            Assert.Equal("sprites/8.png", cut.Find(".summary-who .sprite img").GetAttribute("src")));
        Assert.Equal(T("Evs.Summary.Label", "Wartortle"), cut.Find(".summary-who span[role=img]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Without_species_the_badge_shows_a_question_mark_and_the_label_a_placeholder()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.CurrentSpecies = "";
        pokemon.Species = [new()];
        Seed(pokemon);

        var cut = Render<EvSummary>();

        Assert.Equal(T("Evs.Summary.Label", T("Pokemon.NoSpecies")), cut.Find(".summary-who span[role=img]").GetAttribute("aria-label"));
        Assert.Equal("?", cut.Find(".summary-who .sprite .badge").TextContent.Trim());
    }

    [Fact]
    public void Marks_done_and_over_targets()
    {
        Seed(Pokemons.Squirtle(targets: [5, 0, 2, 0, 0, 0]).WithEvents(("Caterpie", 10, 1), ("Geodude", 2, 1)));

        var cut = Render<EvSummary>();
        var minis = cut.FindAll(".mini");

        Assert.Contains("over", minis[0].ClassList);
        Assert.Contains("done", minis[2].ClassList);
        Assert.DoesNotContain("done", minis[1].ClassList);
        Assert.DoesNotContain("over", minis[1].ClassList);
    }

    [Fact]
    public void The_edit_button_sits_in_the_header_next_to_the_totals()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 10, 1)));

        var cut = Render<EvSummary>();

        var button = Assert.Single(cut.FindAll(".summary button.edit-evs"));
        Assert.Equal(T("Evs.Edit"), button.TextContent.Trim());
        Assert.Contains("card-head", button.ParentElement!.ClassList);
        Assert.Contains("totals", button.PreviousElementSibling!.ClassList);
    }

    [Fact]
    public void The_edit_button_hides_while_the_editor_is_open_and_comes_back()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 10, 1)));
        var cut = Render<EvSummary>();

        cut.Find("button.edit-evs").Click();

        Assert.Empty(cut.FindAll("button.edit-evs"));
        Assert.Contains("summary-grid", cut.Find(".ev-editor").PreviousElementSibling!.ClassList);

        cut.Find(".ev-editor button.cancel").Click();

        Assert.Empty(cut.FindAll(".ev-editor"));
        Assert.Single(cut.FindAll(".card-head button.edit-evs"));
    }
}