using AngleSharp.Dom;

using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components;

/// <summary>
/// Layout regions (contracts/layout.md): the markup keeps the phone order and only CSS rearranges it on
/// wider windows, so these tests pin the regions and their order.
/// </summary>
public class LayoutRegionsTests : AppTestContext
{
    [Fact]
    public void Main_lives_in_the_app_body_and_the_header_does_not()
    {
        Seed(Pokemons.Squirtle());

        var cut = Render<Home>();

        Assert.NotNull(cut.Find(".app-body > main.page"));
        Assert.Null(cut.Find("header.app-header").Closest(".app-body"));
    }

    [Fact]
    public void Empty_state_uses_the_same_structure()
    {
        Store.Load();

        var cut = Render<Home>();

        Assert.NotNull(cut.Find(".app-body > main.page .empty"));
    }

    /// <summary>Direct children of a region, as the class each one is identified by.</summary>
    private static string[] Children(IElement region, params string[] classes) =>
        [.. region.Children.Select(child => classes.FirstOrDefault(child.ClassList.Contains) ?? child.ClassName ?? child.LocalName)];

    [Fact]
    public void Ev_tab_keeps_the_phone_order_inside_its_layout()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 3, 1)));

        var cut = Render<Home>();

        var layout = cut.Find(".ev-layout");
        Assert.Equal(
            ["ev-summary-area", "search-row", "recent-list", "main-list", "history-card"],
            Children(layout, "ev-summary-area", "search-row", "recent-list", "main-list", "history-card"));
        Assert.NotNull(layout.QuerySelector(".ev-summary-area > .summary"));
    }

    [Fact]
    public void Iv_tab_has_table_known_ivs_and_form_in_order()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.IvLog.Add(new EvTrainer.Web.Domain.Model.LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 11], Evs = new int[6] });
        Seed(pokemon);
        var cut = Render<Home>();

        cut.FindAll("[role=tab]")[1].Click();

        var layout = cut.Find(".iv-layout");
        Assert.Equal(3, layout.Children.Length);
        Assert.NotNull(layout.Children[0].QuerySelector(".iv-table"));
        Assert.Contains("known-ivs", layout.Children[1].ClassList);
        Assert.Contains("level-form", layout.Children[2].ClassList);
    }

    [Fact]
    public void Settings_tab_keeps_form_backup_import_and_delete_in_order()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<Home>();

        cut.FindAll("[role=tab]")[2].Click();

        var layout = cut.Find(".settings-layout");
        Assert.Equal(4, layout.Children.Length);
        Assert.Contains("settings", layout.Children[0].ClassList);
        Assert.NotNull(layout.Children[1].QuerySelector("button.export"));
        Assert.Contains("import-card", layout.Children[2].ClassList);
        Assert.Contains("delete-card", layout.Children[3].ClassList);
    }
}