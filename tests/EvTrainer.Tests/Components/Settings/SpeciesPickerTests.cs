using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Settings;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Components.Settings;

public class SpeciesPickerTests : AppTestContext
{
    private SpeciesPick? _picked;

    private IRenderedComponent<SpeciesPicker> RenderPicker(IReadOnlyList<SpeciesEntry>? line = null) =>
        Render<SpeciesPicker>(p => p
            .Add(c => c.Line, line ?? [])
            .Add(c => c.OnPick, pick => _picked = pick));

    [Fact]
    public void Searching_lists_matching_species()
    {
        var cut = RenderPicker();

        cut.Find("input.species-search").Input("squir");

        Assert.Equal("Squirtle", cut.Find("button.species-option").GetAttribute("data-name"));
        Assert.Equal(T("Settings.Species.SearchLabel"), cut.Find("input.species-search").GetAttribute("aria-label"));
    }

    [Fact]
    public void Picking_fills_the_line_and_base_stats_without_typing_them()
    {
        var cut = RenderPicker();
        cut.Find("input.species-search").Input("wartortle");

        cut.Find("button.species-option[data-name='Wartortle']").Click();

        Assert.NotNull(_picked);
        Assert.Equal("Wartortle", _picked!.Picked);
        Assert.Equal(["Squirtle", "Wartortle", "Blastoise"], _picked.Line.Select(s => s.Name));
        Assert.Equal([59, 63, 80, 65, 80, 58], _picked.Line[1].Base);
    }

    [Fact]
    public void Chosen_line_is_shown_with_its_base_stats_and_no_inputs()
    {
        var dex = Web.Domain.Reference.Pokedex.LoadEmbedded();
        var line = dex.LineFor(dex.Find("Squirtle")!).Select(s => new SpeciesEntry { Name = s.Name, Base = [.. s.Base] }).ToList();

        var cut = RenderPicker(line);

        Assert.Equal(["Squirtle", "Wartortle", "Blastoise"], cut.FindAll(".species-chip .name").Select(e => e.TextContent.Trim()));
        Assert.Equal("44 48 65 50 64 43", cut.FindAll(".species-chip .base-stats")[0].TextContent.Trim());
        Assert.Empty(cut.FindAll("input"));
        Assert.Empty(cut.FindAll("button.evolution-option"));
    }

    [Fact]
    public void Branching_species_offer_their_evolutions()
    {
        var cut = RenderPicker([new SpeciesEntry { Name = "Eevee", Base = [55, 55, 50, 45, 65, 55] }]);

        var options = cut.FindAll("button.evolution-option");
        Assert.Equal(5, options.Count);

        cut.Find("button.evolution-option[data-name='Vaporeon']").Click();

        Assert.Equal(["Eevee", "Vaporeon"], _picked!.Line.Select(s => s.Name));
        Assert.Equal("Vaporeon", _picked.Picked);
    }

    [Fact]
    public void Change_species_goes_back_to_search()
    {
        var cut = RenderPicker([new SpeciesEntry { Name = "Eevee", Base = [55, 55, 50, 45, 65, 55] }]);

        cut.Find("button.change-species").Click();

        Assert.NotNull(cut.Find("input.species-search"));
    }

    [Fact]
    public void Options_and_line_show_48px_sprites()
    {
        var cut = RenderPicker();
        cut.Find("input.species-search").Input("squir");

        Assert.All(cut.FindAll("button.species-option"), o =>
            Assert.Contains("--sprite-size: 48px", o.QuerySelector(".sprite")!.GetAttribute("style")));

        cut.Find("button.species-option[data-name='Squirtle']").Click();
        var line = RenderPicker(_picked!.Line);

        Assert.Equal(
            ["sprites/7.png", "sprites/8.png", "sprites/9.png"],
            line.FindAll(".species-chip .sprite img").Select(i => i.GetAttribute("src")));
    }
}