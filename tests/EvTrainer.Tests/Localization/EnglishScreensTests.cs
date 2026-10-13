using AngleSharp.Dom;

using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Localization;

/// <summary>Spec 011 SC-001: with the app in English, no screen shows Portuguese words.</summary>
public class EnglishScreensTests : AppTestContext
{
    public EnglishScreensTests() => Language.Current = "en";

    [Fact]
    public void Tabs_menu_search_and_manual_edit_show_no_portuguese()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 3, 1), ("Abra", 1, 2));
        pokemon.Events.Add(new BattleEvent { Opponent = "", SetTotals = [12, 0, 2, 26, 0, 4] });
        pokemon.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 11], Evs = new int[6] });
        pokemon.KnownIvs[5] = 10;
        Seed(pokemon);
        var cut = Render<Home>();

        for (var tab = 0; tab < 3; tab++)
        {
            cut.FindAll("[role=tab]")[tab].Click();
            AssertNoPortuguese(cut.Nodes);
        }

        cut.Find("header button.menu").Click();
        AssertNoPortuguese(cut.Nodes);

        cut.FindAll("[role=tab]")[0].Click();
        cut.Find("input.search").Input("pewter gym");
        Assert.NotEmpty(cut.FindAll("button.opponent"));
        AssertNoPortuguese(cut.Nodes);

        cut.Find("input.search").Input("");
        cut.Find("button.edit-evs").Click();
        cut.Find("input[type=radio][value=erase]").Change("erase");
        cut.Find(".ev-editor button.save").Click();
        AssertNoPortuguese(cut.Nodes);
    }

    [Fact]
    public void Exp_share_choice_and_shared_toast_show_no_portuguese()
    {
        Seed(Pokemons.Squirtle(), "main");
        var pidgey = Pokemons.Pidgey();
        pidgey.Order = 2;
        Seed(pidgey, "pidgey");
        Store.Select("main");
        Store.SetExpShare("pidgey");
        var cut = Render<Home>();

        cut.Find(".segmented button.all").Click();
        cut.Find("button.opponent[data-opp='Caterpie']").Click();
        AssertNoPortuguese(cut.Nodes);

        Store.Select("pidgey");
        AssertNoPortuguese(cut.Nodes);
    }

    [Fact]
    public void Training_guide_shows_no_portuguese()
    {
        Seed(Pokemons.Squirtle(targets: [252, 0, 0, 0, 0, 0]));
        var cut = Render<Home>();
        cut.Find("button.open-guide").Click();
        cut.Find(".guide-phase button[data-phase='after']").Click();

        foreach (var stat in new[] { 0, 1, 3, 5 })
        {
            cut.Find($".guide-stat button[data-stat='{stat}']").Click();
            AssertNoPortuguese(cut.Nodes);
        }
    }

    [Fact]
    public void Capture_and_sync_show_no_portuguese()
    {
        Store.Load();
        var cut = Render<Home>();
        AssertNoPortuguese(cut.Nodes);

        cut.Find(".empty button.capture").Click();
        cut.Find(".capture-view input.species-search").Input("Squirtle");
        cut.Find(".capture-view button.species-option[data-name='Squirtle']").Click();
        cut.Find("#capture-level").Input("5");
        cut.Find("select#capture-nature").Change("Timid");
        int[] stats = [99, 8, 11, 10, 11, 9];
        for (var i = 0; i < stats.Length; i++)
        {
            cut.Find($"#capture-stat-{i}").Input(stats[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        AssertNoPortuguese(cut.Nodes);

        cut.Find(".capture-view button.close-capture").Click();
        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel button.sync").Click();
        AssertNoPortuguese(cut.Nodes);
    }

    private static void AssertNoPortuguese(INodeList nodes)
    {
        var texts = nodes.SelectMany(n => n.GetDescendantsAndSelf())
            .SelectMany(n => n switch
            {
                IText text when text.ParentElement?.LocalName is not ("style" or "script") => [text.Data],
                IElement element => new[] { "aria-label", "title", "placeholder" }.Select(element.GetAttribute).OfType<string>(),
                _ => [],
            })
            .Where(t => t.Trim().Length > 0);

        var offending = texts.Where(t => KeyParityTests.PortugueseWords.Any(w => KeyParityTests.ContainsWord(t, w))).Distinct().ToList();
        Assert.True(offending.Count == 0, "Portuguese on an English screen: " + string.Join(" | ", offending));
    }
}