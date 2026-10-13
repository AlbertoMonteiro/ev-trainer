using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components.Guide;

/// <summary>Spec 013: the "where to train" page.</summary>
public class TrainingGuideViewTests : AppTestContext
{
    private IRenderedComponent<Home> OpenFromMenu()
    {
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel button.guide").Click();
        return cut;
    }

    private static void ChooseStat(IRenderedComponent<Home> cut, int stat) =>
        cut.Find($".guide-stat button[data-stat='{stat}']").Click();

    [Fact]
    public void Opens_from_the_menu_and_from_the_ev_tab_and_closes()
    {
        Seed(Pokemons.Squirtle());
        var cut = OpenFromMenu();
        Assert.Equal(T("Guide.Title"), cut.Find(".guide-view h2").TextContent.Trim());

        cut.Find(".guide-view button.close-guide").Click();
        Assert.Empty(cut.FindAll(".guide-view"));

        cut.Find("button.open-guide").Click();
        Assert.NotEmpty(cut.FindAll(".guide-view"));
    }

    [Fact]
    public void Sp_attack_lists_bernie_first_with_his_stages_and_evs()
    {
        Store.Load();
        var cut = OpenFromMenu();

        ChooseStat(cut, 3);

        Assert.Equal("true", cut.Find(".guide-stat button[data-stat='3']").GetAttribute("aria-pressed"));
        var first = cut.FindAll("ol.guide-list > li.guide-entry")[0];
        Assert.Equal("ENGINEER_BERNIE", first.GetAttribute("data-trainer"));
        Assert.Equal(T("Guide.Trainer", "Engineer", "Bernie"), first.QuerySelector(".who")!.TextContent.Trim());
        Assert.Equal("Rota 11", first.QuerySelector(".where")!.TextContent.Trim());
        var stages = first.QuerySelectorAll("ol.stages > li.stage");
        Assert.Equal(["0", "3"], stages.Select(s => s.GetAttribute("data-index")));
        Assert.Equal(T("Guide.Stage.0"), stages[0].QuerySelector(".label")!.TextContent.Trim());
        Assert.Equal(T("Guide.Stage.3"), stages[1].QuerySelector(".label")!.TextContent.Trim());
        Assert.Contains("best", stages[1].ClassList);
        Assert.Contains(T("Guide.Pokemon", "Magneton", 28), stages[1].QuerySelector(".team")!.TextContent);
        Assert.Equal(T("Evs.Gain.Part", 6, T("Stat.Short.3")), stages[1].QuerySelector(".evs .main")!.TextContent.Trim());
    }

    [Fact]
    public void Hp_lists_trainers_of_the_same_route_as_separate_entries_and_marks_double_battles()
    {
        Store.Load();
        var cut = OpenFromMenu();
        ChooseStat(cut, 0);

        var twins = cut.Find("li.guide-entry[data-trainer='TWINS_ELI_ANNE']");
        var julia = cut.Find("li.guide-entry[data-trainer='LASS_JULIA']");
        Assert.Equal(T("Guide.Double"), twins.QuerySelector(".double")!.TextContent.Trim());
        Assert.Null(julia.QuerySelector(".double"));
        Assert.Equal(T("Evs.Gain.Part", 4, T("Stat.Short.0")), julia.QuerySelector(".stage.best .evs .main")!.TextContent.Trim());
    }

    [Fact]
    public void Other_stats_show_as_also_and_access_conditions_are_marked()
    {
        Store.Load();
        var cut = OpenFromMenu();
        ChooseStat(cut, 0);

        var grace = cut.Find("li.guide-entry[data-trainer='BEAUTY_GRACE']");
        Assert.Contains(T("Stat.Short.5"), grace.QuerySelector(".stage.best .evs .also")!.TextContent);

        ChooseStat(cut, 1);
        Assert.NotEmpty(cut.FindAll("li.guide-entry .access-surf"));
        Assert.Equal(T("Guide.Access.surf"), cut.Find("li.guide-entry .access-surf").TextContent.Trim());
    }

    [Fact]
    public void Phase_hides_or_shows_the_elite_four_stages_and_is_saved()
    {
        Store.Load();
        var cut = OpenFromMenu();
        ChooseStat(cut, 0);
        Assert.Equal("true", cut.Find(".guide-phase button[data-phase='before']").GetAttribute("aria-pressed"));
        var gia = cut.Find("li.guide-entry[data-trainer='YOUNG_COUPLE_GIA_JES']");
        Assert.Equal(["0", "3"], gia.QuerySelectorAll("li.stage").Select(s => s.GetAttribute("data-index")));

        cut.Find(".guide-phase button[data-phase='after']").Click();

        Assert.Equal("after", Repository.Preferences.GuidePhase);
        gia = cut.Find("li.guide-entry[data-trainer='YOUNG_COUPLE_GIA_JES']");
        var stages = gia.QuerySelectorAll("li.stage");
        Assert.Equal(["0", "3", "5"], stages.Select(s => s.GetAttribute("data-index")));
        Assert.Contains("requires-e4", stages[2].ClassList);
        Assert.Contains("best", stages[2].ClassList);
    }

    [Fact]
    public void Opens_on_the_stat_the_selected_pokemon_needs_most_and_warns_about_wasted_evs()
    {
        Seed(Pokemons.Squirtle(targets: [252, 0, 0, 0, 0, 0]));
        var cut = OpenFromMenu();

        Assert.Equal("true", cut.Find(".guide-stat button[data-stat='0']").GetAttribute("aria-pressed"));
        var grace = cut.Find("li.guide-entry[data-trainer='BEAUTY_GRACE']");
        Assert.Equal(T("Guide.Waste", T("Stat.Long.5")), grace.QuerySelector(".waste")!.TextContent.Trim());
        Assert.Null(cut.Find("li.guide-entry[data-trainer='LASS_JULIA']").QuerySelector(".waste"));

        // The chosen stat is never reported as wasted, even when this Pokémon has no target for it.
        cut.Find(".guide-stat button[data-stat='3']").Click();
        Assert.DoesNotContain(cut.FindAll(".waste"), w => w.TextContent.Contains(T("Stat.Long.3"), StringComparison.Ordinal));
    }

    [Fact]
    public void Without_a_pokemon_it_opens_on_hp_and_shows_tips_and_sources()
    {
        Store.Load();
        var cut = OpenFromMenu();

        Assert.Equal("true", cut.Find(".guide-stat button[data-stat='0']").GetAttribute("aria-pressed"));
        var tips = cut.Find(".guide-tips").TextContent;
        Assert.Contains(T("Guide.Tips.VsSeeker"), tips);
        Assert.Contains(T("Guide.Tips.Sevii"), tips);
        Assert.Equal("https://github.com/pret/pokefirered", cut.Find(".guide-sources a").GetAttribute("href"));
    }
}