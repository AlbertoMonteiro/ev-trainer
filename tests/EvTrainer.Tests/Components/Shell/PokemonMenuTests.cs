using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Localization;
using EvTrainer.Web.Pages;

using Microsoft.AspNetCore.Components.Web;

namespace EvTrainer.Tests.Components.Shell;

public class PokemonMenuTests : AppTestContext
{
    private IRenderedComponent<Home> RenderWithTwo()
    {
        var first = Pokemons.Squirtle().WithEvents(("Caterpie", 82, 1));
        first.Order = 1;
        var second = Pokemons.Squirtle();
        second.Name = "Pidgey";
        second.Order = 2;
        Repository.Stored["a"] = first;
        Repository.Stored["b"] = second;
        Store.Load();
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();
        return cut;
    }

    [Fact]
    public void Lists_pokemon_by_order_with_totals_and_marks_the_selected()
    {
        var cut = RenderWithTwo();

        var items = cut.FindAll(".menu-panel button.pokemon-item");
        Assert.Equal(["a", "b"], items.Select(i => i.GetAttribute("data-id")));
        Assert.Contains("Squirtle", items[0].TextContent);
        Assert.Contains(T("Menu.Total", 82), items[0].TextContent);
        Assert.Equal("true", items[0].GetAttribute("aria-current"));
        Assert.Equal("false", items[1].GetAttribute("aria-current"));
    }

    [Fact]
    public void Choosing_switches_the_active_pokemon_and_closes()
    {
        var cut = RenderWithTwo();

        cut.Find(".menu-panel button.pokemon-item[data-id='b']").Click();

        Assert.Equal("b", Store.SelectedId);
        Assert.Empty(cut.FindAll(".menu-panel"));
    }

    [Fact]
    public void Closes_on_outside_click_and_escape()
    {
        var cut = RenderWithTwo();
        cut.Find(".menu-backdrop").Click();
        Assert.Empty(cut.FindAll(".menu-panel"));

        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll(".menu-panel"));
    }

    [Fact]
    public void Has_add_backup_and_theme_items_but_no_fullscreen()
    {
        var cut = RenderWithTwo();
        var text = cut.Find(".menu-panel").TextContent;

        Assert.Contains(T("Menu.AddPokemon"), text);
        Assert.Contains(T("Menu.ExportBackup"), text);
        Assert.Contains(T("Menu.ImportBackup"), text);
        Assert.Contains(T("Menu.Theme"), text);
        Assert.DoesNotContain("Tela cheia", text);
        Assert.Empty(cut.FindAll(".menu-panel .languages"));
    }

    [Fact]
    public void Theme_choice_is_applied_and_remembered()
    {
        var cut = RenderWithTwo();

        cut.Find(".menu-panel .themes button[data-theme='dark']").Click();

        Assert.Equal("dark", Browser.Theme);
        Assert.Equal("dark", Repository.Preferences.Theme);
        cut.Find("header button.menu").Click();
        Assert.Equal("true", cut.Find(".menu-panel .themes button[data-theme='dark']").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Export_from_the_menu_downloads_the_backup()
    {
        var cut = RenderWithTwo();

        cut.Find(".menu-panel button.export").Click();

        Assert.Single(Browser.Downloads);
    }

    [Fact]
    public void Add_from_the_menu_opens_settings_for_the_new_pokemon()
    {
        var cut = RenderWithTwo();

        cut.Find(".menu-panel button.add").Click();

        Assert.Equal(3, Store.Ordered.Count);
        Assert.Equal("true", cut.FindAll("[role=tab]")[2].GetAttribute("aria-selected"));
    }
}

public class PokemonMenuSpriteTests : AppTestContext
{
    [Fact]
    public void Items_show_the_display_species_sprite_and_the_footer_credits()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.IvLog.Add(new EvTrainer.Web.Domain.Model.LevelEntry { Level = 16, Species = "Wartortle", Stats = [1, 1, 1, 1, 1, 1], Evs = new int[6] });
        Seed(pokemon);
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();

        Assert.Equal("sprites/8.png", cut.Find(".menu-panel button.pokemon-item .sprite img").GetAttribute("src"));
        var credits = cut.Find(".menu-panel .menu-credits");
        Assert.Contains(T("Menu.Credits"), credits.TextContent);
        var link = credits.QuerySelector("a")!;
        Assert.Equal("https://github.com/PokeAPI/sprites", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Contains("noopener", link.GetAttribute("rel"));
        Assert.Equal(T("Menu.Version", EvTrainer.Web.AppInfo.DisplayVersion), cut.Find(".menu-panel .menu-version").TextContent.Trim());
    }
}

public class PokemonMenuLanguageTests : AppTestContext
{
    public PokemonMenuLanguageTests()
        : base(new LocalizationCatalog(new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["pt-BR"] = LocalizationCatalog.LoadEmbedded().Strings("pt-BR"),
            ["en"] = LocalizationCatalog.LoadEmbedded().Strings("pt-BR").ToDictionary(kv => kv.Key, kv => "EN " + kv.Value),
        }))
    {
    }

    [Fact]
    public void Language_choice_appears_with_two_languages_and_switches_live()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();

        cut.Find(".menu-panel .languages button[data-lang='en']").Click();

        Assert.Equal("en", Language.Current);
        Assert.Equal("en", Browser.Language);
        Assert.Equal("en", Repository.Preferences.Language);
        Assert.StartsWith("EN ", cut.FindAll("[role=tab]")[0].TextContent.Trim());
    }
}