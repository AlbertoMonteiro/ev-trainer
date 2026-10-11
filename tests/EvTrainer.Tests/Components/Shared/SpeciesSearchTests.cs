using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Shared;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Components.Shared;

public class SpeciesSearchTests : AppTestContext
{
    private PokedexSpecies? _picked;

    private IRenderedComponent<SpeciesSearch> RenderSearch() =>
        Render<SpeciesSearch>(p => p.Add(c => c.OnPick, species => _picked = species));

    [Fact]
    public void Finds_by_name_ignoring_accents_and_case()
    {
        var cut = RenderSearch();

        cut.Find("input.species-search").Input("ÁBRA");

        var option = cut.Find("button.species-option");
        Assert.Equal("Abra", option.GetAttribute("data-name"));
        Assert.Contains(T("Settings.Species.Dex", 63), option.TextContent);
        Assert.NotNull(option.QuerySelector(".sprite"));
    }

    [Fact]
    public void Finds_by_number_and_picks()
    {
        var cut = RenderSearch();

        cut.Find("input.species-search").Input("#63");
        cut.Find("button.species-option[data-name='Abra']").Click();

        Assert.Equal("Abra", _picked?.Name);
    }
}