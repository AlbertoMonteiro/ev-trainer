using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components.Shell;

/// <summary>Desktop side panel with the Pokémon in training (spec 007, US3; contracts/layout.md).</summary>
public class PokemonSidebarTests : AppTestContext
{
    private IRenderedComponent<Home> RenderWithThree()
    {
        var first = Pokemons.Squirtle().WithEvents(("Caterpie", 82, 1));
        first.Order = 1;
        var second = Pokemons.Squirtle();
        second.CurrentSpecies = "Wartortle";
        second.Order = 2;
        var third = Pokemons.Squirtle();
        third.CurrentSpecies = "Blastoise";
        third.Order = 3;
        Repository.Stored["a"] = first;
        Repository.Stored["b"] = second;
        Repository.Stored["c"] = third;
        Store.Load();
        return Render<Home>();
    }

    [Fact]
    public void Lists_every_pokemon_in_order_with_sprite_and_total()
    {
        var cut = RenderWithThree();

        var aside = cut.Find("aside.pokemon-sidebar");
        Assert.Equal(T("Sidebar.Title"), aside.GetAttribute("aria-label"));
        Assert.Equal(T("Sidebar.Title"), aside.QuerySelector("h2")!.TextContent.Trim());
        var items = aside.QuerySelectorAll("button.pokemon-item");
        Assert.Equal(["a", "b", "c"], items.Select(i => i.GetAttribute("data-id")));
        Assert.NotNull(items[0].QuerySelector(".sprite"));
        Assert.Contains(T("Menu.Total", 82), items[0].TextContent);
        Assert.Contains("Blastoise", items[2].TextContent);
        Assert.Equal("true", items[0].GetAttribute("aria-current"));
        Assert.Equal("false", items[2].GetAttribute("aria-current"));
        Assert.Null(items[0].GetAttribute("role"));
    }

    [Fact]
    public void Clicking_selects_and_keeps_the_current_tab()
    {
        var cut = RenderWithThree();
        cut.FindAll("[role=tab]")[1].Click();

        cut.Find("aside.pokemon-sidebar button.pokemon-item[data-id='c']").Click();

        Assert.Equal("c", Store.SelectedId);
        Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
        Assert.Equal("true", cut.Find("aside.pokemon-sidebar button.pokemon-item[data-id='c']").GetAttribute("aria-current"));
    }

    [Fact]
    public void Choosing_a_pokemon_closes_the_capture_screen()
    {
        var cut = RenderWithThree();
        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel button.capture").Click();
        Assert.NotEmpty(cut.FindAll(".capture-view"));

        cut.Find("aside.pokemon-sidebar button.pokemon-item[data-id='b']").Click();

        Assert.Empty(cut.FindAll(".capture-view"));
        Assert.NotEmpty(cut.FindAll("[role=tablist]"));
        Assert.Equal("b", Store.SelectedId);
    }

    [Fact]
    public void Add_creates_a_pokemon_and_opens_its_settings()
    {
        var cut = RenderWithThree();

        var add = cut.Find("aside.pokemon-sidebar button.add");
        Assert.Equal(T("Menu.AddPokemon"), add.TextContent.Trim());
        add.Click();

        Assert.Equal(4, Repository.Stored.Count);
        Assert.Equal("true", cut.FindAll("[role=tab]")[2].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Without_pokemon_only_the_add_button_is_shown()
    {
        Store.Load();

        var cut = Render<Home>();

        var aside = cut.Find("aside.pokemon-sidebar");
        Assert.Empty(aside.QuerySelectorAll("button.pokemon-item"));
        Assert.NotNull(aside.QuerySelector("button.add"));
    }

    [Fact]
    public void Sidebar_sits_before_main_inside_the_app_body()
    {
        var cut = RenderWithThree();

        var body = cut.Find(".app-body");
        Assert.Equal(["aside", "main"], body.Children.Select(c => c.LocalName));
    }

    [Fact]
    public void Lists_only_the_party_and_opens_the_party_page()
    {
        for (var i = 1; i <= 7; i++)
        {
            var pokemon = Pokemons.Pidgey();
            pokemon.Order = i;
            pokemon.Party = i != 4;
            Repository.Stored[$"p{i}"] = pokemon;
        }

        Store.Load();
        var cut = Render<Home>();

        var items = cut.FindAll("aside.pokemon-sidebar .pokemon-list:not(.opened) button.pokemon-item");
        Assert.Equal(["p1", "p2", "p3", "p5", "p6", "p7"], items.Select(i => i.GetAttribute("data-id")));

        cut.Find("aside.pokemon-sidebar button.manage-party").Click();

        Assert.NotNull(cut.Find(".party-view"));
    }
}