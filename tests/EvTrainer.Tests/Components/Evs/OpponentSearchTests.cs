using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Evs;

namespace EvTrainer.Tests.Components.Evs;

public class OpponentSearchTests : AppTestContext
{
    private static List<string> Names(IRenderedComponent<EvTab> cut, string section) =>
        [.. cut.FindAll($"{section} button.opponent").Select(b => b.GetAttribute("data-opp")!)];

    [Fact]
    public void Useful_is_the_default_and_the_choice_is_remembered()
    {
        Seed(Pokemons.Squirtle(targets: [252, 0, 2, 252, 0, 4]).WithEvents(("Geodude", 2, 1)));
        var cut = Render<EvTab>();

        Assert.Equal("true", cut.Find(".segmented button.useful").GetAttribute("aria-pressed"));
        Assert.Equal(T("Evs.List.Useful"), cut.Find(".main-list .section-title").TextContent.Trim());
        Assert.DoesNotContain("Geodude", Names(cut, ".main-list"));
        Assert.DoesNotContain("Ivysaur", Names(cut, ".main-list"));
        Assert.Contains("Caterpie", Names(cut, ".main-list"));

        cut.Find(".segmented button.all").Click();

        Assert.Equal("all", Repository.Preferences.Filter);
        Assert.Equal(T("Evs.List.All"), cut.Find(".main-list .section-title").TextContent.Trim());
        Assert.Contains("Ivysaur", Names(cut, ".main-list"));
    }

    [Fact]
    public void Recents_show_without_search_and_are_not_repeated()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 1, 1), ("Abra", 1, 1)));
        var cut = Render<EvTab>();

        Assert.Equal(["Abra", "Caterpie"], Names(cut, ".recent-list"));
        Assert.DoesNotContain("Caterpie", Names(cut, ".main-list"));
        Assert.Equal(T("Evs.List.Recent"), cut.Find(".recent-list .section-title").TextContent.Trim());
    }

    [Fact]
    public void Searching_ignores_the_filter_hides_recents_and_shows_trainers()
    {
        Seed(Pokemons.Squirtle(targets: [0, 0, 0, 0, 0, 0]).WithEvents(("Caterpie", 1, 1)));
        var cut = Render<EvTab>();

        cut.Find("input.search").Input("rota 4");

        Assert.Empty(cut.FindAll(".recent-list"));
        Assert.Equal(T("Evs.List.Results"), cut.Find(".main-list .section-title").TextContent.Trim());
        Assert.Contains("Mankey", Names(cut, ".main-list"));
        var paras = cut.Find(".main-list button.opponent[data-opp='Paras'] .trainers");
        Assert.Equal(T("Evs.Card.Trainers", "Rota 4"), paras.TextContent.Trim());
        Assert.Empty(cut.FindAll(".main-list button.opponent[data-opp='Mankey'] .trainers"));
    }

    [Fact]
    public void Empty_states_span_both_columns()
    {
        Seed(Pokemons.Squirtle(targets: [0, 0, 0, 0, 0, 0]));
        var cut = Render<EvTab>();

        Assert.Equal(T("Empty.NoUseful"), cut.Find(".main-list .grid-empty").TextContent.Trim());

        cut.Find("input.search").Input("zzzz");

        Assert.Equal(T("Empty.NoResults"), cut.Find(".main-list .grid-empty").TextContent.Trim());
    }

    [Fact]
    public void Search_input_is_labelled()
    {
        Seed(Pokemons.Squirtle());

        var cut = Render<EvTab>();

        Assert.Equal(T("Evs.Search.Label"), cut.Find("input.search").GetAttribute("aria-label"));
        Assert.Equal(T("Evs.Search.Placeholder"), cut.Find("input.search").GetAttribute("placeholder"));
    }

    [Fact]
    public void Every_card_in_recents_list_and_results_has_a_sprite()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 1, 1), ("Abra", 1, 1)));
        var cut = Render<EvTab>();

        Assert.All(cut.FindAll(".recent-list button.opponent, .main-list button.opponent"), b => Assert.NotNull(b.QuerySelector(".sprite")));

        cut.Find("input.search").Input("rota 4");

        var results = cut.FindAll(".main-list button.opponent");
        Assert.NotEmpty(results);
        Assert.All(results, b => Assert.NotNull(b.QuerySelector(".sprite")));
    }

    [Fact]
    public void Searching_milo_finds_and_records_milotic()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<EvTab>();

        cut.Find("input.search").Input("milo");
        var card = cut.Find(".main-list button.opponent[data-opp='Milotic']");

        Assert.Equal(T("Evs.Card.Yield", 2, T("Stat.Short.4")), card.QuerySelector(".ev-tag")!.TextContent.Trim());
        Assert.Equal("sprites/350.png", card.QuerySelector(".sprite img")!.GetAttribute("src"));

        card.Click();

        Assert.Equal(T("Toast.EvGain", "Milotic", T("Evs.Gain.Part", 2, T("Stat.Long.4"))), Toasts.Message);
        Assert.Equal(["Milotic"], Repository.Stored["p1"].Events.Select(e => e.Opponent));
    }

    [Fact]
    public void All_lists_the_386_species_in_dex_order_then_the_vitamins()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<EvTab>();

        cut.Find(".segmented button.all").Click();
        var names = Names(cut, ".main-list");

        Assert.Equal(392, names.Count);
        Assert.Equal(Data.Opponents.Select(o => o.Name), names);
        Assert.Equal("Bulbasaur", names[0]);
        Assert.Equal("Deoxys", names[385]);
        Assert.Equal("Carbos", names[^1]);
    }
}