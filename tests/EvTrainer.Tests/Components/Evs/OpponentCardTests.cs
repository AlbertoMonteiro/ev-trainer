using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Evs;

namespace EvTrainer.Tests.Components.Evs;

public class OpponentCardTests : AppTestContext
{
    [Fact]
    public void Whole_card_is_a_button_with_name_yield_and_count()
    {
        var picked = "";
        var abra = Data.Lookup("Abra")!;

        var cut = Render<OpponentCard>(p => p
            .Add(c => c.Opponent, abra)
            .Add(c => c.Count, 7)
            .Add(c => c.OnPick, name => picked = name));

        var button = cut.Find("button.opponent");
        Assert.Equal("Abra", button.QuerySelector(".name")!.TextContent.Trim());
        Assert.Equal(T("Evs.Card.Yield", 1, T("Stat.Short.3")), button.QuerySelector(".ev-tag")!.TextContent.Trim());
        Assert.Equal(T("Evs.Card.Count", 7), button.QuerySelector(".count")!.TextContent.Trim());

        button.Click();
        Assert.Equal("Abra", picked);
    }

    [Fact]
    public void Does_not_show_location_or_dex_number()
    {
        var pikachu = Data.Lookup("Pikachu")!;

        var cut = Render<OpponentCard>(p => p.Add(c => c.Opponent, pikachu));

        Assert.DoesNotContain("#25", cut.Markup);
        Assert.DoesNotContain("Viridian", cut.Markup);
        Assert.Empty(cut.FindAll(".trainers"));
    }

    [Fact]
    public void Shows_trainer_locations_when_given()
    {
        var paras = Data.Lookup("Paras")!;

        var cut = Render<OpponentCard>(p => p
            .Add(c => c.Opponent, paras)
            .Add(c => c.TrainerLocations, ["Rota 4"]));

        Assert.Equal(T("Evs.Card.Trainers", "Rota 4"), cut.Find(".trainers").TextContent.Trim());
    }
}