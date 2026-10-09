using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Ivs;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Components.Ivs;

public class IvTableTests : AppTestContext
{
    private static LevelEntry Level(int lv, int[] st) => new() { Level = lv, Species = "Squirtle", Stats = st, Evs = new int[6] };

    [Fact]
    public void Shows_final_row_and_one_row_per_level()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        pokemon.IvLog.Add(Level(5, [20, 9, 12, 11, 12, 11]));
        Seed(pokemon);

        var cut = Render<IvTable>();

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("final", rows[0].ClassList);
        Assert.Equal(T("Ivs.Row.Final"), rows[0].QuerySelector("th")!.TextContent.Trim());
        Assert.Equal(T("Ivs.Row.Level", 5), rows[1].QuerySelector("th")!.TextContent.Trim());
        Assert.Equal(["12–31", "4–31", "10–29", "20–31", "12–31", "14–31"], rows[1].QuerySelectorAll("td.iv").Select(c => c.TextContent.Trim()));
    }

    [Fact]
    public void Marks_inconsistent_cells_and_explains_them()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        pokemon.IvLog.Add(Level(5, [99, 9, 12, 11, 12, 0]));
        Seed(pokemon);

        var cut = Render<IvTable>();
        var cells = cut.FindAll("tbody tr")[1].QuerySelectorAll("td.iv");

        Assert.Equal(T("Ivs.Cell.Inconsistent"), cells[0].TextContent.Trim());
        Assert.Contains("bad", cells[0].ClassList);
        Assert.Equal(T("Ivs.Cell.NoData"), cells[5].TextContent.Trim());
        Assert.Equal(T("Ivs.InconsistentNote"), cut.Find(".iv-note").TextContent.Trim());
    }

    [Fact]
    public void Final_shows_check_and_no_data_states()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        pokemon.IvLog.Add(Level(5, [20, 9, 12, 11, 12, 0]));
        pokemon.IvLog.Add(Level(5, [19, 9, 12, 11, 12, 0]));
        Seed(pokemon);

        var cut = Render<IvTable>();
        var final = cut.Find("tbody tr.final").QuerySelectorAll("td.iv");

        Assert.Equal(T("Ivs.Final.Conflict"), final[0].TextContent.Trim());
        Assert.Equal(T("Ivs.Final.NoData"), final[5].TextContent.Trim());
    }

    [Fact]
    public void Removing_a_level_offers_undo()
    {
        var pokemon = Pokemons.Squirtle("Timid");
        pokemon.IvLog.Add(Level(5, [20, 9, 12, 11, 12, 11]));
        Seed(pokemon);
        var cut = Render<IvTable>();

        cut.Find("button.remove").Click();

        Assert.Empty(Repository.Stored["p1"].IvLog);
        Assert.Equal(T("Ivs.Removed", 5), Toasts.Message);

        cut.InvokeAsync(Toasts.RunUndo);

        Assert.Single(Repository.Stored["p1"].IvLog);
    }
}