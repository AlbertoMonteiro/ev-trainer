using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Evs;

namespace EvTrainer.Tests.Components.Evs;

/// <summary>Feature 002, US2: when no image can be shown, every slot keeps a correct badge and its size.</summary>
public class SpriteFallbackTests : AppTestContext
{
    [Fact]
    public void Every_card_falls_back_to_its_badge_without_changing_size()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 1, 1), ("Calcium", 1, 1)));
        var cut = Render<EvTab>();
        cut.Find(".segmented button.all").Click();

        foreach (var img in cut.FindAll(".opponent-grid img").ToList())
        {
            img.TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());
        }

        Assert.Empty(cut.FindAll(".opponent-grid img"));
        var sprites = cut.FindAll(".opponent-grid .sprite");
        Assert.NotEmpty(sprites);
        Assert.All(sprites, s =>
        {
            Assert.Contains("--sprite-size: 40px", s.GetAttribute("style"));
            Assert.False(string.IsNullOrWhiteSpace(s.QuerySelector(".badge")!.TextContent));
        });

        string Badge(string name) => cut.Find($"button.opponent[data-opp='{name}'] .badge").TextContent.Trim();
        Assert.Equal(T("Sprite.Dex", 10), Badge("Caterpie"));
        Assert.Equal("C", Badge("Calcium"));
        Assert.Contains("badge-dark", cut.Find("button.opponent[data-opp='Geodude'] .sprite").ClassList);
    }
}