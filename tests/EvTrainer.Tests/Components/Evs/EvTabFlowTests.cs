using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components.Evs;

public class EvTabFlowTests : AppTestContext
{
    private IRenderedComponent<Home> RenderWithAll()
    {
        var cut = Render<Home>();
        // US1 lists every opponent; US3 adds the "Úteis" filter, so force "Todos" when present.
        cut.FindAll(".segmented button").LastOrDefault()?.Click();
        return cut;
    }

    private static void Tap(IRenderedComponent<Home> cut, string name) =>
        cut.Find($".opponent-grid button.opponent[data-opp='{name}']").Click();

    [Fact]
    public void Reaching_a_target_moves_the_opponent_out_of_the_useful_results()
    {
        Seed(Pokemons.Squirtle(targets: [0, 0, 0, 0, 0, 1]));
        var cut = Render<Home>();
        cut.Find("input.search").Input("pidgey");
        Assert.Equal([T("Evs.List.Useful")], Titles(cut));

        Tap(cut, "Pidgey");

        Assert.Equal([T("Evs.List.Results")], Titles(cut));

        cut.Find(".toast button").Click();

        Assert.Equal([T("Evs.List.Useful")], Titles(cut));
    }

    private static List<string> Titles(IRenderedComponent<Home> cut) =>
        [.. cut.FindAll(".main-list .section-title").Select(t => t.TextContent.Trim())];

    [Fact]
    public void Tapping_records_the_battle_shows_the_gain_and_saves()
    {
        Seed(Pokemons.Squirtle(targets: [252, 0, 2, 252, 0, 4]));
        var cut = RenderWithAll();

        Tap(cut, "Caterpie");

        Assert.Equal("1/252", cut.FindAll(".mini .value")[0].TextContent.Trim());
        Assert.Equal(T("Evs.Card.Count", 1), cut.Find(".opponent-grid button.opponent[data-opp='Caterpie'] .count").TextContent.Trim());
        Assert.Equal(T("Toast.EvGain", "Caterpie", T("Evs.Gain.Part", 1, T("Stat.Long.0"))), cut.Find(".toast .msg").TextContent);
        Assert.Equal(1, Repository.Stored["p1"].Events.Sum(e => e.Count));
    }

    [Fact]
    public void Undo_reverts_the_battle()
    {
        Seed(Pokemons.Squirtle());
        var cut = RenderWithAll();
        Tap(cut, "Caterpie");

        cut.Find(".toast button").Click();

        Assert.Equal("0/252", cut.FindAll(".mini .value")[0].TextContent.Trim());
        Assert.Empty(Repository.Stored["p1"].Events);
    }

    [Fact]
    public void Pokerus_and_macho_brace_give_four_times()
    {
        Seed(Pokemons.Squirtle());
        var cut = RenderWithAll();
        var toggles = cut.FindAll("header button.toggle");
        Assert.Equal(["false", "false"], toggles.Select(t => t.GetAttribute("aria-pressed")));

        cut.FindAll("header button.toggle")[0].Click();
        cut.FindAll("header button.toggle")[1].Click();
        Tap(cut, "Caterpie");

        toggles = cut.FindAll("header button.toggle");
        Assert.Equal(["true", "true"], toggles.Select(t => t.GetAttribute("aria-pressed")));
        Assert.All(toggles, t => Assert.Contains(T("Header.Double"), t.TextContent));
        Assert.Equal("4/252", cut.FindAll(".mini .value")[0].TextContent.Trim());
    }

    [Fact]
    public void At_the_cap_the_toast_says_no_ev()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 255, 1)));
        var cut = RenderWithAll();

        Tap(cut, "Caterpie");

        Assert.Equal(T("Toast.NoEvGain", "Caterpie"), cut.Find(".toast .msg").TextContent);
    }
}
public class EvTabExpShareFlowTests : AppTestContext
{
    private IRenderedComponent<Home> RenderWithHolder(EvTrainer.Web.Domain.Model.TrainingPokemon holder)
    {
        Seed(Pokemons.Squirtle(), "main");
        holder.Order = 2;
        Seed(holder, "pidgey");
        Store.Select("main");
        Store.SetExpShare("pidgey");
        var cut = Render<Home>();
        cut.FindAll(".segmented button").LastOrDefault()?.Click();
        return cut;
    }

    [Fact]
    public void The_toast_shows_both_gains_and_undo_reverts_both()
    {
        var cut = RenderWithHolder(Pokemons.Pidgey());

        cut.Find(".opponent-grid button.opponent[data-opp='Caterpie']").Click();

        var hp = T("Evs.Gain.Part", 1, T("Stat.Long.0"));
        Assert.Equal(
            T("Toast.EvGain", "Caterpie", hp) + " · " + T("Toast.ExpShareGain", "Pidgey", hp),
            cut.Find(".toast .msg").TextContent);

        cut.Find(".toast button").Click();

        Assert.Empty(Store.All["main"].Events);
        Assert.Empty(Store.All["pidgey"].Events);
    }

    [Fact]
    public void A_full_holder_is_reported_in_the_toast()
    {
        var full = Pokemons.Pidgey();
        full.Events.Add(new EvTrainer.Web.Domain.Model.BattleEvent { Opponent = "", SetTotals = [255, 255, 0, 0, 0, 0] });
        var cut = RenderWithHolder(full);

        cut.Find(".opponent-grid button.opponent[data-opp='Caterpie']").Click();

        Assert.EndsWith(" · " + T("Toast.ExpShareNoGain", "Pidgey"), cut.Find(".toast .msg").TextContent);
    }
}