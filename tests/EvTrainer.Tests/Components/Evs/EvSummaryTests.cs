using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Evs;

namespace EvTrainer.Tests.Components.Evs;

public class EvSummaryTests : AppTestContext
{
    [Fact]
    public void Shows_title_totals_and_six_mini_cards()
    {
        Seed(Pokemons.Squirtle(targets: [252, 0, 2, 252, 0, 4]).WithEvents(("Caterpie", 10, 1), ("Geodude", 2, 1)));

        var cut = Render<EvSummary>();

        Assert.Equal(T("Evs.Summary.Title", "Squirtle"), cut.Find(".card-head h2").TextContent.Trim());
        Assert.Equal(T("Evs.Summary.Totals", 12, 510), cut.Find(".card-head .totals").TextContent.Trim());
        var minis = cut.FindAll(".mini");
        Assert.Equal(6, minis.Count);
        Assert.Equal(T("Stat.Short.0"), minis[0].QuerySelector(".label")!.TextContent.Trim());
        Assert.Equal("10/252", minis[0].QuerySelector(".value")!.TextContent.Trim());
        Assert.Equal("2/2", minis[2].QuerySelector(".value")!.TextContent.Trim());
    }

    [Fact]
    public void Marks_done_and_over_targets()
    {
        Seed(Pokemons.Squirtle(targets: [5, 0, 2, 0, 0, 0]).WithEvents(("Caterpie", 10, 1), ("Geodude", 2, 1)));

        var cut = Render<EvSummary>();
        var minis = cut.FindAll(".mini");

        Assert.Contains("over", minis[0].ClassList);
        Assert.Contains("done", minis[2].ClassList);
        Assert.DoesNotContain("done", minis[1].ClassList);
        Assert.DoesNotContain("over", minis[1].ClassList);
    }
}