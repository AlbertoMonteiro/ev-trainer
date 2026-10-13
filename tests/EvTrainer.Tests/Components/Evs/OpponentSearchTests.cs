using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Evs;
using EvTrainer.Web.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace EvTrainer.Tests.Components.Evs;

public class OpponentSearchTests : AppTestContext
{
    private static List<string> Names(IRenderedComponent<EvTab> cut, string section) =>
        [.. cut.FindAll($"{section} button.opponent").Select(b => b.GetAttribute("data-opp")!)];

    private static List<string> Titles(IRenderedComponent<EvTab> cut) =>
        [.. cut.FindAll(".main-list .section-title").Select(t => t.TextContent.Trim())];

    /// <summary>Names per grid of the main list, in order (one entry per rendered group).</summary>
    private static List<List<string>> Groups(IRenderedComponent<EvTab> cut) =>
        [.. cut.FindAll(".main-list .opponent-grid").Select(g => g.QuerySelectorAll("button.opponent").Select(b => b.GetAttribute("data-opp")!).ToList())];

    private static bool Useful(string name, int[] totals, int[] targets) => EvTrainer.Web.Domain.Evs.IsUseful(Data.Lookup(name)!, totals, targets);

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
        Assert.Equal([T("Evs.List.Useful"), T("Evs.List.Others")], Titles(cut));
        Assert.Contains("Ivysaur", Groups(cut)[1]);
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

    [Theory]
    [InlineData("en", "pewter gym", "Pewter Gym (Brock)")]
    [InlineData("pt-BR", "ginásio de pewter", "Ginásio de Pewter (Brock)")]
    public void Trainer_places_show_in_the_active_language(string language, string query, string place)
    {
        Language.Current = language;
        Seed(Pokemons.Squirtle());
        var cut = Render<EvTab>();

        cut.Find("input.search").Input(query);

        var onix = cut.Find(".main-list button.opponent[data-opp='Onix'] .trainers");
        Assert.Equal(T("Evs.Card.Trainers", place), onix.TextContent.Trim());
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
        Seed(Pokemons.Squirtle(targets: [0, 0, 0, 0, 0, 0]));
        var cut = Render<EvTab>();

        cut.Find(".segmented button.all").Click();
        var names = Names(cut, ".main-list");

        Assert.Equal([T("Evs.List.All")], Titles(cut));
        Assert.Equal(392, names.Count);
        Assert.Equal(Data.Opponents.Select(o => o.Name), names);
        Assert.Equal("Bulbasaur", names[0]);
        Assert.Equal("Deoxys", names[385]);
        Assert.Equal("Carbos", names[^1]);
    }

    [Fact]
    public void All_shows_the_useful_first_then_the_others_each_in_dex_order()
    {
        int[] targets = [252, 0, 2, 252, 0, 4], totals = [0, 0, 0, 0, 0, 0];
        Seed(Pokemons.Squirtle(targets: targets));
        var cut = Render<EvTab>();

        cut.Find(".segmented button.all").Click();
        var groups = Groups(cut);

        Assert.Equal([T("Evs.List.Useful"), T("Evs.List.Others")], Titles(cut));
        Assert.Equal(392, groups.Sum(g => g.Count));
        Assert.Equal(Data.Opponents.Select(o => o.Name).Where(n => Useful(n, totals, targets)), groups[0]);
        Assert.Equal(Data.Opponents.Select(o => o.Name).Where(n => !Useful(n, totals, targets)), groups[1]);
        Assert.Equal("Carbos", groups[0][^1]);
    }

    [Fact]
    public void Search_shows_the_useful_first_then_the_other_results()
    {
        int[] targets = [252, 0, 2, 252, 0, 4], totals = [0, 0, 0, 0, 0, 0];
        Seed(Pokemons.Squirtle(targets: targets));
        var cut = Render<EvTab>();

        cut.Find("input.search").Input("rota 4");
        var found = Services.GetRequiredService<OpponentSearch>().Find("rota 4", GameVersion.FireRed).Select(h => h.Opponent.Name).ToList();
        var groups = Groups(cut);

        Assert.Equal([T("Evs.List.Useful"), T("Evs.List.OtherResults")], Titles(cut));
        Assert.Equal(found.Where(n => Useful(n, totals, targets)), groups[0]);
        Assert.Equal(found.Where(n => !Useful(n, totals, targets)), groups[1]);
        Assert.Contains("Spearow", groups[0]);
        Assert.Contains("Mankey", groups[1]);
        var paras = cut.Find(".main-list button.opponent[data-opp='Paras'] .trainers");
        Assert.Equal(T("Evs.Card.Trainers", "Rota 4"), paras.TextContent.Trim());
    }

    [Fact]
    public void Search_where_everything_is_useful_shows_only_the_useful_group()
    {
        Seed(Pokemons.Squirtle(targets: [0, 0, 0, 0, 0, 252]));
        var cut = Render<EvTab>();

        cut.Find("input.search").Input("pidgey");

        Assert.Equal([T("Evs.List.Useful")], Titles(cut));
        Assert.Equal(["Pidgey"], Groups(cut).Single());
    }

    [Fact]
    public void Search_without_useful_results_keeps_the_results_title()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Geodude", 2, 1)));
        var cut = Render<EvTab>();

        cut.Find("input.search").Input("geodude");

        Assert.Equal([T("Evs.List.Results")], Titles(cut));
        Assert.Equal(["Geodude"], Groups(cut).Single());
    }

    [Theory]
    [InlineData("useful")]
    [InlineData("all")]
    public void While_searching_the_filter_is_disabled_and_nothing_is_pressed(string filter)
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<EvTab>();
        cut.Find($".segmented button.{filter}").Click();

        cut.Find("input.search").Input("rota 4");

        Assert.All(cut.FindAll(".segmented button"), b =>
        {
            Assert.True(b.HasAttribute("disabled"));
            Assert.Equal("false", b.GetAttribute("aria-pressed"));
        });
        Assert.Equal(filter, Repository.Preferences.Filter);

        cut.Find("input.search").Input("");

        Assert.All(cut.FindAll(".segmented button"), b => Assert.False(b.HasAttribute("disabled")));
        Assert.Equal("true", cut.Find($".segmented button.{filter}").GetAttribute("aria-pressed"));
    }
}