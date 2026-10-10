using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Evs;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Components.Evs;

public class RecentHistoryTests : AppTestContext
{
    [Fact]
    public void Shows_the_last_12_events_newest_first_with_count_and_multiplier()
    {
        var pokemon = Pokemons.Squirtle();
        for (var i = 0; i < 14; i++)
        {
            pokemon.WithEvents((i % 2 == 0 ? "Caterpie" : "Abra", i + 1, i == 13 ? 2 : 1));
        }

        Seed(pokemon);

        var cut = Render<RecentHistory>();

        Assert.Equal(T("History.Title"), cut.Find("details summary").TextContent.Trim());
        var rows = cut.FindAll(".history li");
        Assert.Equal(12, rows.Count);
        Assert.Equal(T("History.Item", "Abra", 14) + " " + T("History.Multiplier", 2), rows[0].QuerySelector(".what")!.TextContent.Trim());
        Assert.Equal(T("History.Item", "Caterpie", 13), rows[1].QuerySelector(".what")!.TextContent.Trim());
    }

    [Fact]
    public void Removing_recomputes_and_offers_undo()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 5, 1), ("Abra", 1, 1)));
        var cut = Render<RecentHistory>();

        cut.FindAll(".history li button.remove")[1].Click();

        Assert.Equal(["Abra"], Repository.Stored["p1"].Events.Select(e => e.Opponent));
        Assert.Equal(T("History.Removed", "Caterpie"), Toasts.Message);

        cut.InvokeAsync(Toasts.RunUndo);

        Assert.Equal(["Caterpie", "Abra"], Repository.Stored["p1"].Events.Select(e => e.Opponent));
    }

    [Fact]
    public void Empty_log_says_so()
    {
        Seed(Pokemons.Squirtle());

        var cut = Render<RecentHistory>();

        Assert.Equal(T("History.Empty"), cut.Find(".history-empty").TextContent.Trim());
    }

    [Fact]
    public void Manual_adjustment_is_listed_and_removable()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 10, 1));
        pokemon.Events.Add(new BattleEvent { Opponent = "", SetTotals = [12, 0, 2, 26, 0, 4] });
        Seed(pokemon);
        var cut = Render<RecentHistory>();

        var first = cut.FindAll(".history li")[0];
        Assert.Equal(T("History.Adjustment", "12/0/2/26/0/4"), first.QuerySelector(".what")!.TextContent.Trim());

        first.QuerySelector("button.remove")!.Click();

        Assert.Equal([10, 0, 0, 0, 0, 0], Store.SelectedTotals);
        Assert.Equal(T("History.AdjustmentRemoved"), Toasts.Message);

        cut.InvokeAsync(Toasts.RunUndo);

        Assert.Equal([12, 0, 2, 26, 0, 4], Store.SelectedTotals);
    }
}