using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components.Shell;

public class AppShellTests : AppTestContext
{
    [Fact]
    public void Without_pokemon_shows_the_empty_state()
    {
        Store.Load();

        var cut = Render<Home>();

        Assert.Contains(T("Empty.NoPokemon"), cut.Find(".empty").TextContent);
        Assert.Equal(T("Empty.AddPokemon"), cut.Find(".empty button.primary").TextContent.Trim());
        Assert.Empty(cut.FindAll("[role=tablist]"));
    }

    [Fact]
    public void Header_has_menu_title_and_status()
    {
        Store.Load();

        var cut = Render<Home>();

        Assert.Equal(T("Menu.Open"), cut.Find("header button.menu").GetAttribute("aria-label"));
        Assert.Equal(T("App.Title"), cut.Find("header h1").TextContent.Trim());
        Assert.NotNull(cut.Find("header [role=status]"));
    }

    [Fact]
    public void Adding_a_pokemon_opens_the_settings_tab()
    {
        Store.Load();
        var cut = Render<Home>();

        cut.Find(".empty button.primary").Click();

        var tabs = cut.FindAll("[role=tab]");
        Assert.Equal([T("Tabs.Evs"), T("Tabs.Ivs"), T("Tabs.Settings")], tabs.Select(t => t.TextContent.Trim()));
        Assert.Equal("true", tabs[2].GetAttribute("aria-selected"));
        Assert.Single(Repository.Stored);
    }

    [Fact]
    public void Tabs_switch_on_click()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<Home>();

        cut.FindAll("[role=tab]")[1].Click();

        Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
        Assert.Equal("false", cut.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));
    }
}