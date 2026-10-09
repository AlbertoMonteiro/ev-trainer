using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Settings;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Components.Settings;

public class SpreadsheetImportPanelTests : AppTestContext
{
    [Fact]
    public void Battles_mode_previews_and_appends_on_confirm()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Abra", 1, 1)));
        var cut = Render<SpreadsheetImportPanel>();

        Assert.Equal(T("Import.Title"), cut.Find("details summary").TextContent.Trim());
        cut.Find("textarea").Input("Caterpie;3;S\nlixo");

        Assert.Equal(T("Import.Preview", 1, 1), cut.Find(".import-preview").TextContent.Trim());
        cut.Find("button.confirm").Click();

        Assert.Equal([("Abra", 1, 1), ("Caterpie", 3, 2)], Repository.Stored["p1"].Events.Select(e => (e.Opponent, e.Count, e.Multiplier)));
        Assert.Equal(T("Import.Done", 1), Toasts.Message);
    }

    [Fact]
    public void Levels_mode_inserts_sorted_by_level()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.IvLog.Add(new LevelEntry { Level = 30, Species = "Wartortle", Stats = [1, 1, 1, 1, 1, 1], Evs = new int[6] });
        Seed(pokemon);
        var cut = Render<SpreadsheetImportPanel>();

        cut.Find(".segmented button.levels").Click();
        cut.Find("textarea").Input("21\tWartortle\t62\t31\t42\t39\t44\t38");
        cut.Find("button.confirm").Click();

        Assert.Equal([21, 30], Repository.Stored["p1"].IvLog.Select(l => l.Level));
    }

    [Fact]
    public void Confirm_is_disabled_without_recognized_lines()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<SpreadsheetImportPanel>();

        cut.Find("textarea").Input("nada");

        Assert.True(cut.Find("button.confirm").HasAttribute("disabled"));
    }
}