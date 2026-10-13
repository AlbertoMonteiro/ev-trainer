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
        var second = Pokemons.Pidgey();
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
    }

    [Fact]
    public void Lists_portuguese_and_english_each_in_its_own_language_with_the_active_one_pressed()
    {
        var cut = RenderWithTwo();

        var buttons = cut.FindAll(".menu-panel .languages button");
        Assert.Equal(["pt-BR", "en"], buttons.Select(b => b.GetAttribute("data-lang")));
        Assert.Equal(["Português (Brasil)", "English"], buttons.Select(b => b.TextContent.Trim()));
        Assert.Equal(["true", "false"], buttons.Select(b => b.GetAttribute("aria-pressed")));
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
    public void Game_version_choice_lists_fire_red_and_leaf_green_and_is_remembered()
    {
        var cut = RenderWithTwo();

        Assert.Contains(T("Menu.GameVersion"), cut.Find(".menu-panel").TextContent);
        var buttons = cut.FindAll(".menu-panel .choices.versions button");
        Assert.Equal(["fireRed", "leafGreen"], buttons.Select(b => b.GetAttribute("data-version")));
        Assert.Equal([T("Version.FireRed"), T("Version.LeafGreen")], buttons.Select(b => b.TextContent.Trim()));
        Assert.Equal(["true", "false"], buttons.Select(b => b.GetAttribute("aria-pressed")));

        cut.Find(".menu-panel .versions button[data-version='leafGreen']").Click();

        Assert.Equal("leafGreen", Repository.Preferences.GameVersion);
        cut.Find("header button.menu").Click();
        Assert.Equal("true", cut.Find(".menu-panel .versions button[data-version='leafGreen']").GetAttribute("aria-pressed"));
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
    public void Items_show_the_species_and_the_nature_to_tell_twins_apart()
    {
        var first = Pokemons.Squirtle(nature: "Timid");
        var second = Pokemons.Squirtle(nature: "Modest");
        second.Order = 2;
        Repository.Stored["a"] = first;
        Repository.Stored["b"] = second;
        Store.Load();
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();

        var items = cut.FindAll(".menu-panel button.pokemon-item");
        Assert.All(items, i => Assert.Contains("Squirtle", i.TextContent));
        Assert.Equal(["Timid", "Modest"], items.Select(i => i.QuerySelector(".nature")!.TextContent.Trim()));
    }

    [Fact]
    public void Item_without_species_shows_a_placeholder_and_no_nature_when_none()
    {
        var pokemon = Pokemons.Squirtle(nature: "");
        pokemon.CurrentSpecies = "";
        pokemon.Species = [new()];
        Seed(pokemon);
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();

        var item = cut.Find(".menu-panel button.pokemon-item");
        Assert.Contains(T("Pokemon.NoSpecies"), item.TextContent);
        Assert.Equal("?", item.QuerySelector(".sprite .badge")!.TextContent.Trim());
        Assert.Null(item.QuerySelector(".nature"));
    }

    [Fact]
    public void Items_show_the_current_species_sprite_and_the_footer_credits()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.CurrentSpecies = "Wartortle";
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
public class LanguageSwitchKeepsStateTests : AppTestContext
{
    [Fact]
    public void Switching_to_english_keeps_the_search_the_tab_and_the_selected_pokemon()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<Home>();
        cut.Find("input.search").Input("caterpie");
        var selected = Store.Selected;

        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel .languages button[data-lang='en']").Click();

        Assert.Equal("en", Language.Current);
        Assert.Equal("caterpie", cut.Find("input.search").GetAttribute("value"));
        Assert.Equal(Catalog.Find("en", "Evs.Search.Placeholder"), cut.Find("input.search").GetAttribute("placeholder"));
        Assert.Equal("true", cut.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));
        Assert.Equal(Catalog.Find("en", "Tabs.Evs"), cut.FindAll("[role=tab]")[0].TextContent.Trim());
        Assert.Same(selected, Store.Selected);
        Assert.NotEmpty(cut.FindAll("button.opponent[data-opp='Caterpie']"));
    }
}

public class GameVersionSwitchTests : AppTestContext
{
    [Fact]
    public void Switching_the_version_keeps_the_search_and_changes_the_wild_pokemon()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<Home>();
        cut.Find("input.search").Input("safari zone");
        Assert.NotEmpty(cut.FindAll("button.opponent[data-opp='Scyther']"));
        Assert.Empty(cut.FindAll("button.opponent[data-opp='Pinsir']"));

        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel .versions button[data-version='leafGreen']").Click();

        Assert.Equal("safari zone", cut.Find("input.search").GetAttribute("value"));
        Assert.NotEmpty(cut.FindAll("button.opponent[data-opp='Pinsir']"));
        Assert.Empty(cut.FindAll("button.opponent[data-opp='Scyther']"));
    }
}