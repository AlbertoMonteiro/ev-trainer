using System.Text.RegularExpressions;

using AngleSharp.Dom;

using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Localization;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Localization;

/// <summary>
/// SC-010: with a synthetic language where every key renders as «key», no visible text or accessible
/// label may be left outside that pattern, except game data (names, locations), numbers and symbols.
/// </summary>
public partial class NoHardcodedTextTests : AppTestContext
{
    private static readonly LocalizationCatalog Synthetic = new(new Dictionary<string, IReadOnlyDictionary<string, string>>
    {
        ["pt-BR"] = LocalizationCatalog.LoadEmbedded().Strings("pt-BR"),
        ["xx"] = LocalizationCatalog.LoadEmbedded().Strings("pt-BR").ToDictionary(kv => kv.Key, kv => $"«{kv.Key}»"),
    });

    private readonly HashSet<string> _data;

    public NoHardcodedTextTests()
        : base(Synthetic)
    {
        _data =
        [
            .. Data.Opponents.Select(o => o.Name),
            .. Data.Natures.Select(n => n.Name),
            .. Data.TrainersByLocation.Select(l => l.Name),
            .. Data.TrainersByLocation.Select(l => Places.Display(l.Name, "en")),
            .. Guide.Trainers.Select(t => t.Location),
            .. Dex.Species.Select(s => s.Name),
            // Sprite badges without a dex number show the initial of a data name (vitamins, unknown species).
            .. Data.Opponents.Select(o => o.Name[..1].ToUpperInvariant()),
            // Each language is listed by its own name (endonym), taken from its resource file.
            .. Synthetic.Languages.Select(l => LocalizationCatalog.LoadEmbedded().Find(l, "Language.Name") ?? Synthetic.Find(l, "Language.Name")!),
        ];
        Language.Current = "xx";
    }

    [Fact]
    public void Empty_state_has_no_hardcoded_text()
    {
        Store.Load();

        var cut = Render<Home>();

        Assert.NotNull(cut.Find("aside.pokemon-sidebar"));
        AssertNoHardcodedText(cut.Nodes);
    }

    [Fact]
    public void Every_tab_and_the_menu_have_no_hardcoded_text()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 3, 1), ("Abra", 1, 2));
        pokemon.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [99, 9, 12, 11, 12, 11], Evs = new int[6] });
        Seed(pokemon);
        var cut = Render<Home>();
        Assert.NotEmpty(cut.FindAll("aside.pokemon-sidebar button.pokemon-item"));

        for (var tab = 0; tab < 3; tab++)
        {
            cut.FindAll("[role=tab]")[tab].Click();
            AssertNoHardcodedText(cut.Nodes);
        }

        cut.Find("header button.menu").Click();
        AssertNoHardcodedText(cut.Nodes);

        cut.FindAll("[role=tab]")[0].Click();
        cut.Find("button.opponent[data-opp='Caterpie']").Click();
        AssertNoHardcodedText(cut.Nodes);
    }

    [Fact]
    public void Grouped_search_and_disabled_filter_have_no_hardcoded_text()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<Home>();

        cut.Find("input.search").Input("rota 4");
        Assert.Equal(2, cut.FindAll(".main-list .section-title").Count);
        AssertNoHardcodedText(cut.Nodes);

        cut.Find("input.search").Input("");
        cut.Find(".segmented button.all").Click();
        Assert.Equal(2, cut.FindAll(".main-list .section-title").Count);
        AssertNoHardcodedText(cut.Nodes);
    }

    [Fact]
    public void Exp_share_choice_and_shared_toast_have_no_hardcoded_text()
    {
        Seed(Pokemons.Squirtle(), "main");
        var pidgey = Pokemons.Pidgey();
        pidgey.Order = 2;
        Seed(pidgey, "pidgey");
        Store.Select("main");
        Store.SetExpShare("pidgey");
        var cut = Render<Home>();

        Assert.NotEmpty(cut.FindAll("select#exp-share"));
        cut.Find(".segmented button.all").Click();
        cut.Find("button.opponent[data-opp='Caterpie']").Click();
        AssertNoHardcodedText(cut.Nodes);

        Store.Select("pidgey");
        AssertNoHardcodedText(cut.Nodes);
    }

    [Fact]
    public void Training_guide_has_no_hardcoded_text()
    {
        Seed(Pokemons.Squirtle(targets: [252, 0, 0, 0, 0, 0]));
        var cut = Render<Home>();
        cut.Find("button.open-guide").Click();
        AssertNoHardcodedText(cut.Nodes);

        cut.Find(".guide-phase button[data-phase='after']").Click();
        foreach (var stat in new[] { 1, 3, 5 })
        {
            cut.Find($".guide-stat button[data-stat='{stat}']").Click();
            AssertNoHardcodedText(cut.Nodes);
        }
    }

    [Fact]
    public void Manual_edit_screens_have_no_hardcoded_text()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 3, 1));
        pokemon.Events.Add(new BattleEvent { Opponent = "", SetTotals = [12, 0, 2, 26, 0, 4] });
        pokemon.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 11], Evs = new int[6] });
        pokemon.KnownIvs[5] = 10;
        Seed(pokemon);
        var cut = Render<Home>();

        cut.Find("button.edit-evs").Click();
        cut.Find("input[type=radio][value=erase]").Change("erase");
        cut.Find(".ev-editor button.save").Click();
        Assert.NotEmpty(cut.FindAll("[role=alertdialog]"));
        AssertNoHardcodedText(cut.Nodes);

        cut.FindAll("[role=tab]")[1].Click();
        cut.Find(".iv-table button.edit").Click();
        Assert.NotEmpty(cut.FindAll(".level-form .cancel-edit"));
        Assert.NotEmpty(cut.FindAll(".known-mismatch"));
        AssertNoHardcodedText(cut.Nodes);
    }

    [Fact]
    public async Task Sync_screen_and_status_have_no_hardcoded_text()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel button.sync").Click();

        AssertNoHardcodedText(cut.Nodes);

        cut.Find(".sync-view input#sync-token").Input("token");
        SyncProvider.FailNext(EvTrainer.Web.State.SyncErrorKind.MissingGistPermission);
        cut.Find(".sync-view button.connect").Click();
        cut.WaitForAssertion(() => cut.Find(".sync-view .error"));
        AssertNoHardcodedText(cut.Nodes);

        cut.Find(".sync-view button.connect").Click();
        cut.WaitForAssertion(() => cut.Find(".sync-view .connected"));
        AssertNoHardcodedText(cut.Nodes);

        SyncProvider.FailNext(EvTrainer.Web.State.SyncErrorKind.InvalidToken);
        await cut.InvokeAsync(Sync.SyncNowAsync);
        Assert.NotEmpty(cut.FindAll(".sync-view button.reconnect"));
        AssertNoHardcodedText(cut.Nodes);

        cut.Find(".sync-view button.disconnect").Click();
        AssertNoHardcodedText(cut.Nodes);
    }

    [Fact]
    public void Capture_screen_has_no_hardcoded_text()
    {
        Store.Load();
        Capture.Conclude(new EvTrainer.Web.Domain.CaptureInput("Squirtle", 5, "Timid", [20, 9, 13, 11, 12, 11]));
        var cut = Render<Home>();

        cut.Find(".empty button.capture").Click();
        AssertNoHardcodedText(cut.Nodes);

        cut.Find(".capture-view input.species-search").Input("Squirtle");
        AssertNoHardcodedText(cut.Nodes);
        cut.Find(".capture-view button.species-option[data-name='Squirtle']").Click();
        cut.Find("#capture-level").Input("5");
        cut.Find("select#capture-nature").Change("Timid");
        int[] stats = [99, 8, 11, 10, 11, 9];
        for (var i = 0; i < stats.Length; i++)
        {
            cut.Find($"#capture-stat-{i}").Input(stats[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        Assert.NotEmpty(cut.FindAll(".capture-view .inconsistent"));
        Assert.NotEmpty(cut.FindAll(".best-capture .compare, .best-capture .summary"));
        AssertNoHardcodedText(cut.Nodes);

        cut.Find("#capture-stat-0").Input("19");
        Assert.NotEmpty(cut.FindAll(".best-capture .compare"));
        AssertNoHardcodedText(cut.Nodes);

        cut.Find(".capture-view button.change-species").Click();
        cut.Find(".capture-view input.species-search").Input("Shedinja");
        cut.Find(".capture-view button.species-option[data-name='Shedinja']").Click();
        cut.Find("select#capture-nature").Change("Hardy");
        cut.Find("#capture-level").Input("25");
        int[] shedinja = [1, 54, 33, 26, 26, 30];
        for (var i = 0; i < shedinja.Length; i++)
        {
            cut.Find($"#capture-stat-{i}").Input(shedinja[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        Assert.NotEmpty(cut.FindAll(".capture-view .unknowable"));
        AssertNoHardcodedText(cut.Nodes);
    }

    private void AssertNoHardcodedText(INodeList nodes)
    {
        var offenders = new List<string>();
        foreach (var node in nodes.SelectMany(n => n.GetDescendantsAndSelf()))
        {
            if (node is IText text && text.ParentElement?.LocalName is not ("style" or "script"))
            {
                Check(text.Data, $"text in <{text.ParentElement?.LocalName}>", offenders);
            }

            if (node is IElement element)
            {
                foreach (var attribute in new[] { "aria-label", "title", "placeholder" })
                {
                    if (element.GetAttribute(attribute) is { } value)
                    {
                        Check(value, $"{attribute} of <{element.LocalName}>", offenders);
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, "Hardcoded text: " + string.Join(" | ", offenders.Distinct()));
    }

    private void Check(string raw, string where, List<string> offenders)
    {
        var value = raw.Trim();
        if (value.Length == 0
            || Localized().IsMatch(value)
            || NumbersAndSymbols().IsMatch(value)
            || _data.Contains(value)
            || value.Split(" ", StringSplitOptions.RemoveEmptyEntries).All(word => Localized().IsMatch(word) || NumbersAndSymbols().IsMatch(word) || _data.Contains(word)))
        {
            return;
        }

        offenders.Add($"{where}: \"{value}\"");
    }

    [GeneratedRegex(@"^(«[A-Za-z0-9_.]+»\s*)+$")]
    private static partial Regex Localized();

    [GeneratedRegex(@"^[\d\s/–\-·×+#●☰()📱🔄⚠️🗙️☁⏳]*$")]
    private static partial Regex NumbersAndSymbols();
}