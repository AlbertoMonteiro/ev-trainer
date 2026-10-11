using System.Globalization;

using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components.Capture;

public class CaptureViewTests : AppTestContext
{
    private static readonly int[] SquirtleStats = [20, 9, 12, 11, 12, 11];

    private IRenderedComponent<Home> OpenFromMenu()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 3, 1)));
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();
        var item = cut.Find(".menu-panel button.capture");
        Assert.Equal(T("Menu.Capture"), item.TextContent.Trim());
        item.Click();
        return cut;
    }

    private static void PickSpecies(IRenderedComponent<Home> cut, string species)
    {
        cut.Find(".capture-view input.species-search").Input(species);
        cut.Find($".capture-view button.species-option[data-name='{species}']").Click();
    }

    private static void TypeStats(IRenderedComponent<Home> cut, int[] stats)
    {
        for (var i = 0; i < stats.Length; i++)
        {
            cut.Find($"#capture-stat-{i}").Input(stats[i].ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void Fill(IRenderedComponent<Home> cut, string species, int level, string nature, int[] stats)
    {
        PickSpecies(cut, species);
        cut.Find("#capture-level").Input(level.ToString(CultureInfo.InvariantCulture));
        cut.Find("select#capture-nature").Change(nature);
        TypeStats(cut, stats);
    }

    private static string[] Column(IRenderedComponent<Home> cut, string cell) =>
        [.. cut.FindAll($".capture-table tbody tr td.{cell}").Select(td => td.TextContent.Trim())];

    [Fact]
    public void Opens_from_the_menu_with_all_fields()
    {
        var cut = OpenFromMenu();

        Assert.Equal(T("Capture.Title"), cut.Find(".capture-view h2").TextContent.Trim());
        Assert.Empty(cut.FindAll("[role=tablist]"));
        Assert.Equal("numeric", cut.Find("#capture-level").GetAttribute("inputmode"));
        Assert.Equal(6, cut.FindAll(".capture-view .capture-stats input").Count);
        Assert.All(cut.FindAll(".capture-view .capture-stats input"), i => Assert.Equal("numeric", i.GetAttribute("inputmode")));
        var natures = cut.FindAll("select#capture-nature option").Where(o => !string.IsNullOrEmpty(o.GetAttribute("value"))).ToList();
        Assert.Equal(25, natures.Count);
        Assert.Contains(natures, o => o.TextContent.Contains("Timid", StringComparison.Ordinal) && o.TextContent.Contains(T("Stat.Short.5"), StringComparison.Ordinal));
        Assert.Empty(cut.FindAll(".capture-table"));
    }

    [Fact]
    public void Opens_from_the_empty_state_and_close_returns_to_it()
    {
        Store.Load();
        var cut = Render<Home>();

        var button = cut.Find(".empty button.capture");
        Assert.Equal(T("Empty.Capture"), button.TextContent.Trim());
        button.Click();
        Assert.NotEmpty(cut.FindAll(".capture-view"));

        cut.Find(".capture-view button.close-capture").Click();

        Assert.Empty(cut.FindAll(".capture-view"));
        Assert.NotEmpty(cut.FindAll(".empty"));
    }

    [Fact]
    public void Reference_case_shows_ranges_grades_verdict_and_nature()
    {
        var cut = OpenFromMenu();
        var saves = Repository.SaveCount;

        Fill(cut, "Squirtle", 5, "Timid", SquirtleStats);

        Assert.Equal(["12–31", "4–31", "10–29", "20–31", "12–31", "14–31"], Column(cut, "range"));
        var grades = Column(cut, "grade");
        Assert.Equal(T("Capture.GradeSpan", T("Capture.Grade.Poor"), T("Capture.Grade.Great")), grades[1]);
        Assert.Equal(T("Capture.GradeSpan", T("Capture.Grade.Good"), T("Capture.Grade.Great")), grades[3]);

        var rows = cut.FindAll(".capture-table tbody tr");
        Assert.Contains("not-counted", rows[1].ClassList);
        Assert.Equal("false", rows[1].QuerySelector("button.count")!.GetAttribute("aria-pressed"));
        Assert.Equal("true", rows[0].QuerySelector("button.count")!.GetAttribute("aria-pressed"));
        Assert.Equal(T("Capture.Counted.Toggle", T("Stat.Long.1")), rows[1].QuerySelector("button.count")!.GetAttribute("aria-label"));

        Assert.Equal(
            T("Capture.Verdict.Uncertain", T("Capture.Verdict.Fair"), T("Capture.Verdict.Excellent")),
            cut.Find(".capture-verdict .verdict").TextContent.Trim());
        Assert.Equal(T("Capture.LowLevelHint"), cut.Find(".capture-verdict .low-level-hint").TextContent.Trim());
        Assert.Equal(T("Capture.Nature.Helps"), cut.Find(".capture-verdict .nature-effect").TextContent.Trim());
        Assert.Equal(saves, Repository.SaveCount);
    }

    [Fact]
    public void Toggling_a_stat_is_remembered_and_reset_restores_the_default()
    {
        var cut = OpenFromMenu();
        Fill(cut, "Squirtle", 5, "Timid", SquirtleStats);

        cut.FindAll(".capture-table tbody tr")[1].QuerySelector("button.count")!.Click();

        Assert.Equal("true", cut.FindAll(".capture-table tbody tr")[1].QuerySelector("button.count")!.GetAttribute("aria-pressed"));
        Assert.True(Capture.CountedFor("Squirtle")[1]);
        Assert.Equal(T("Capture.Nature.Harms"), cut.Find(".capture-verdict .nature-effect").TextContent.Trim());

        cut.Find(".capture-view button.reset-counted").Click();

        Assert.False(Capture.CountedFor("Squirtle")[1]);
        Assert.Contains("not-counted", cut.FindAll(".capture-table tbody tr")[1].ClassList);
    }

    [Fact]
    public void Invalid_fields_show_no_result()
    {
        var cut = OpenFromMenu();
        Fill(cut, "Squirtle", 5, "Timid", SquirtleStats);

        cut.Find("#capture-level").Input("0");

        Assert.Equal("true", cut.Find("#capture-level").GetAttribute("aria-invalid"));
        Assert.Empty(cut.FindAll(".capture-table"));

        cut.Find("#capture-level").Input("5");
        cut.Find("#capture-stat-2").Input("abc");

        Assert.Equal("true", cut.Find("#capture-stat-2").GetAttribute("aria-invalid"));
        Assert.Empty(cut.FindAll(".capture-table"));
    }

    [Fact]
    public void Impossible_stat_is_explained()
    {
        var cut = OpenFromMenu();

        Fill(cut, "Squirtle", 5, "Timid", [99, 9, 12, 11, 12, 11]);

        Assert.Equal("?", Column(cut, "range")[0]);
        Assert.Equal(T("Capture.Inconsistent"), cut.Find(".capture-view .inconsistent").TextContent.Trim());
        Assert.Equal(T("Capture.Verdict.Inconsistent"), cut.Find(".capture-verdict .verdict").TextContent.Trim());
    }

    [Fact]
    public void Shedinja_hp_cannot_be_known()
    {
        var cut = OpenFromMenu();

        Fill(cut, "Shedinja", 25, "Hardy", [1, 54, 33, 26, 26, 30]);

        Assert.Equal(T("Capture.Unknowable"), Column(cut, "range")[0]);
        Assert.Contains("not-counted", cut.FindAll(".capture-table tbody tr")[0].ClassList);
    }

    [Fact]
    public void New_capture_keeps_species_level_and_nature()
    {
        var cut = OpenFromMenu();
        Fill(cut, "Squirtle", 5, "Timid", SquirtleStats);

        cut.Find(".capture-view button.new-capture").Click();

        Assert.Equal("Squirtle", cut.Find(".capture-species").GetAttribute("data-name"));
        Assert.Equal("5", cut.Find("#capture-level").GetAttribute("value"));
        Assert.Equal("Timid", cut.Find("select#capture-nature").GetAttribute("value"));
        Assert.All(cut.FindAll(".capture-stats input"), i => Assert.Equal("", i.GetAttribute("value") ?? ""));
        Assert.Empty(cut.FindAll(".capture-table"));
    }

    [Fact]
    public void Change_species_reopens_the_search()
    {
        var cut = OpenFromMenu();
        PickSpecies(cut, "Squirtle");

        cut.Find(".capture-view button.change-species").Click();

        Assert.NotEmpty(cut.FindAll(".capture-view input.species-search"));
    }

    [Fact]
    public void Close_returns_to_the_tabs_without_touching_the_list()
    {
        var cut = OpenFromMenu();
        Fill(cut, "Squirtle", 5, "Timid", SquirtleStats);
        var saves = Repository.SaveCount;

        cut.Find(".capture-view button.close-capture").Click();

        Assert.Empty(cut.FindAll(".capture-view"));
        Assert.NotEmpty(cut.FindAll("[role=tablist]"));
        Assert.Equal(saves, Repository.SaveCount);
        Assert.Single(Repository.Stored);
    }

    [Fact]
    public void Add_is_disabled_until_the_capture_is_valid()
    {
        var cut = OpenFromMenu();
        PickSpecies(cut, "Squirtle");

        Assert.Equal(T("Capture.Add"), cut.Find(".capture-view button.add-capture").TextContent.Trim());
        Assert.True(cut.Find(".capture-view button.add-capture").HasAttribute("disabled"));
    }

    [Fact]
    public void Adding_opens_the_iv_tab_with_the_same_ranges()
    {
        var cut = OpenFromMenu();
        Fill(cut, "Squirtle", 5, "Timid", SquirtleStats);

        cut.Find(".capture-view button.add-capture").Click();

        Assert.Equal(2, Repository.Stored.Count);
        Assert.Empty(cut.FindAll(".capture-view"));
        Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
        var rows = cut.FindAll(".iv-table tbody tr");
        Assert.Equal(["12–31", "4–31", "10–29", "20–31", "12–31", "14–31"], rows[1].QuerySelectorAll("td.iv").Select(c => c.TextContent.Trim()));
        Assert.Equal(["12–31", "4–31", "10–29", "20–31", "12–31", "14–31"], rows[0].QuerySelectorAll("td.iv").Select(c => c.TextContent.Trim()));
        Assert.Equal(T("Capture.Added", "Squirtle"), Toasts.Message);

        cut.InvokeAsync(Toasts.RunUndo);

        Assert.Single(Repository.Stored);
    }

    private static readonly int[] WeakStats = [19, 8, 11, 10, 11, 9];
    private static readonly int[] StrongStats = [20, 9, 13, 11, 12, 11];

    [Fact]
    public void Without_a_best_the_card_says_so_and_new_capture_keeps_it()
    {
        var cut = OpenFromMenu();
        Fill(cut, "Squirtle", 5, "Timid", WeakStats);

        Assert.Equal(T("Capture.Best.None", "Squirtle"), cut.Find(".best-capture .none").TextContent.Trim());
        Assert.Equal(T("Capture.Best.WillSave"), cut.Find(".best-capture .will-save").TextContent.Trim());
        Assert.Null(Capture.BestFor("Squirtle"));

        cut.Find(".capture-view button.new-capture").Click();

        Assert.Equal(WeakStats, Capture.BestFor("Squirtle")!.Stats);
        Assert.Equal(T("Capture.Best.Saved", "Squirtle"), Toasts.Message);
    }

    [Fact]
    public void Better_capture_is_flagged_live_and_replaces_the_best_when_closing()
    {
        Capture.Conclude(new EvTrainer.Web.Domain.CaptureInput("Squirtle", 5, "Timid", WeakStats));
        var cut = OpenFromMenu();
        Fill(cut, "Squirtle", 5, "Timid", StrongStats);

        var card = cut.Find(".best-capture");
        Assert.Equal(T("Capture.Best.Title"), card.QuerySelector("h3")!.TextContent.Trim());
        Assert.Contains(T("Capture.Compare.Better"), card.QuerySelector(".compare")!.TextContent);
        Assert.Equal(T("Capture.Best.WillSave"), card.QuerySelector(".will-save")!.TextContent.Trim());
        Assert.Equal(WeakStats, Capture.BestFor("Squirtle")!.Stats);

        cut.Find(".capture-view button.close-capture").Click();

        Assert.Equal(StrongStats, Capture.BestFor("Squirtle")!.Stats);
        Assert.Equal(T("Capture.Best.Saved", "Squirtle"), Toasts.Message);
        cut.InvokeAsync(Toasts.RunUndo);
        Assert.Equal(WeakStats, Capture.BestFor("Squirtle")!.Stats);
    }

    [Fact]
    public void Worse_capture_is_flagged_and_changes_nothing()
    {
        Capture.Conclude(new EvTrainer.Web.Domain.CaptureInput("Squirtle", 5, "Timid", StrongStats));
        var cut = OpenFromMenu();
        Fill(cut, "Squirtle", 5, "Timid", WeakStats);

        Assert.Contains(T("Capture.Compare.Worse"), cut.Find(".best-capture .compare").TextContent);
        Assert.Empty(cut.FindAll(".best-capture .will-save"));
        Assert.Contains(T("Capture.Best.Summary", 5, "Timid", ""), cut.Find(".best-capture .summary").TextContent.Trim());

        cut.Find(".capture-view button.new-capture").Click();

        Assert.Equal(StrongStats, Capture.BestFor("Squirtle")!.Stats);
    }

    [Fact]
    public void Forget_and_add_from_the_best()
    {
        Capture.Conclude(new EvTrainer.Web.Domain.CaptureInput("Squirtle", 5, "Timid", StrongStats));
        var cut = OpenFromMenu();
        PickSpecies(cut, "Squirtle");

        Assert.Equal(T("Capture.Best.Add"), cut.Find(".best-capture button.add-best").TextContent.Trim());
        cut.Find(".best-capture button.add-best").Click();

        Assert.Equal(2, Repository.Stored.Count);
        Assert.Empty(cut.FindAll(".capture-view"));

        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel button.capture").Click();
        PickSpecies(cut, "Squirtle");
        cut.Find(".best-capture button.forget").Click();

        Assert.Null(Capture.BestFor("Squirtle"));
        Assert.Equal(T("Capture.Best.Forgotten", "Squirtle"), Toasts.Message);
        cut.InvokeAsync(Toasts.RunUndo);
        Assert.NotNull(Capture.BestFor("Squirtle"));
    }

    [Fact]
    public void Adding_also_concludes_the_capture()
    {
        var cut = OpenFromMenu();
        Fill(cut, "Squirtle", 5, "Timid", WeakStats);

        cut.Find(".capture-view button.add-capture").Click();

        Assert.Equal(WeakStats, Capture.BestFor("Squirtle")!.Stats);
    }
}