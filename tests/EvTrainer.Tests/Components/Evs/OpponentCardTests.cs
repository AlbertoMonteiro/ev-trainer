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

        // The dex number only exists inside the sprite's fallback badge (feature 002, FR-006),
        // which is hidden under the image; the card's own text never shows it.
        var cardText = cut.Find("button.opponent .info").TextContent;
        Assert.DoesNotContain("#25", cardText);
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

    [Fact]
    public void Shows_the_sprite_before_the_name_and_stays_one_button()
    {
        var picked = "";
        var cut = Render<OpponentCard>(p => p
            .Add(c => c.Opponent, Data.Lookup("Abra")!)
            .Add(c => c.OnPick, name => picked = name));

        var button = cut.Find("button.opponent");
        Assert.Equal("sprites/63.png", button.QuerySelector(".sprite img")!.GetAttribute("src"));
        Assert.True(button.QuerySelector(".sprite")!.CompareDocumentPosition(button.QuerySelector(".name")!)
            .HasFlag(AngleSharp.Dom.DocumentPositions.Following));
        Assert.Single(cut.FindAll("button"));

        button.Click();
        Assert.Equal("Abra", picked);
    }

    [Fact]
    public void Vitamin_card_shows_only_the_badge()
    {
        var cut = Render<OpponentCard>(p => p.Add(c => c.Opponent, Data.Lookup("Calcium")!));

        Assert.NotNull(cut.Find("button.opponent .sprite .badge"));
        Assert.Empty(cut.FindAll("img"));
    }
}