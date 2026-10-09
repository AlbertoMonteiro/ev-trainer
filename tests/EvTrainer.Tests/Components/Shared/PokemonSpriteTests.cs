using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Shared;

using Microsoft.AspNetCore.Components.Web;

namespace EvTrainer.Tests.Components.Shared;

public class PokemonSpriteTests : AppTestContext
{
    private IRenderedComponent<PokemonSprite> RenderSprite(string name, int size = 40) =>
        Render<PokemonSprite>(p => p.Add(c => c.Name, name).Add(c => c.Size, size));

    [Fact]
    public void Renders_badge_and_lazy_image_with_fixed_size()
    {
        var cut = RenderSprite("Caterpie");

        var sprite = cut.Find("span.sprite");
        Assert.Equal("true", sprite.GetAttribute("aria-hidden"));
        Assert.Contains("--sprite-size: 40px", sprite.GetAttribute("style"));
        Assert.Contains("--badge: var(--stat-0)", sprite.GetAttribute("style"));
        Assert.Equal(T("Sprite.Dex", 10), cut.Find(".badge").TextContent.Trim());

        var img = cut.Find("img");
        Assert.Equal("sprites/10.png", img.GetAttribute("src"));
        Assert.Equal("", img.GetAttribute("alt"));
        Assert.Equal("40", img.GetAttribute("width"));
        Assert.Equal("40", img.GetAttribute("height"));
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Equal("async", img.GetAttribute("decoding"));
        Assert.DoesNotContain("loaded", img.ClassList);
    }

    [Fact]
    public void Load_shows_the_image()
    {
        var cut = RenderSprite("Caterpie");

        cut.Find("img").TriggerEvent("onload", new ProgressEventArgs());

        Assert.Contains("loaded", cut.Find("img").ClassList);
    }

    [Fact]
    public void Error_keeps_only_the_badge()
    {
        var cut = RenderSprite("Caterpie");

        cut.Find("img").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        Assert.Empty(cut.FindAll("img"));
        Assert.Equal(T("Sprite.Dex", 10), cut.Find(".badge").TextContent.Trim());
    }

    [Fact]
    public void Two_stats_make_a_gradient()
    {
        var cut = RenderSprite("Ivysaur");

        Assert.Contains("--badge: linear-gradient(135deg, var(--stat-3), var(--stat-4))", cut.Find("span.sprite").GetAttribute("style"));
    }

    [Fact]
    public void No_ev_data_uses_the_accent()
    {
        var cut = RenderSprite("Treecko", 48);

        Assert.Contains("--badge: var(--accent)", cut.Find("span.sprite").GetAttribute("style"));
        Assert.Contains("--sprite-size: 48px", cut.Find("span.sprite").GetAttribute("style"));
        Assert.Equal("sprites/252.png", cut.Find("img").GetAttribute("src"));
    }

    [Fact]
    public void Vitamin_has_only_the_badge_with_its_initial()
    {
        var cut = RenderSprite("Calcium");

        Assert.Empty(cut.FindAll("img"));
        Assert.Equal("C", cut.Find(".badge").TextContent.Trim());
    }
}