using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class SpriteBadgeTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    private static SpriteBadge For(string name) => SpriteBadge.For(name, Data, Dex);

    [Fact]
    public void Single_stat_pokemon()
    {
        var badge = For("Caterpie");

        Assert.Equal(10, badge.Dex);
        Assert.Equal([0], badge.Stats);
        Assert.True(badge.HasSprite);
    }

    [Fact]
    public void Two_stats_in_index_order_when_yields_tie() => Assert.Equal([3, 4], For("Ivysaur").Stats);

    [Fact]
    public void Two_stats_ordered_by_yield_then_index() => Assert.Equal([3, 4], For("Venusaur").Stats);

    [Fact]
    public void Aliases_resolve_to_the_canonical_species() => Assert.Equal(29, For("Nidoran F").Dex);

    [Fact]
    public void Vitamin_has_stat_color_initial_and_no_sprite()
    {
        var badge = For("Calcium");

        Assert.Null(badge.Dex);
        Assert.Equal([3], badge.Stats);
        Assert.Equal("C", badge.Initial);
        Assert.False(badge.HasSprite);
    }

    [Fact]
    public void Species_beyond_kanto_use_their_own_ev_color()
    {
        // Feature 003: every species #1–386 is an opponent with an EV yield.
        var badge = For("Treecko");

        Assert.Equal(252, badge.Dex);
        Assert.Equal([5], badge.Stats);
        Assert.True(badge.HasSprite);
    }

    [Fact]
    public void Unknown_name_uses_the_initial()
    {
        var badge = For("Agumon");

        Assert.Null(badge.Dex);
        Assert.Empty(badge.Stats);
        Assert.Equal("A", badge.Initial);
        Assert.False(badge.HasSprite);
    }
}