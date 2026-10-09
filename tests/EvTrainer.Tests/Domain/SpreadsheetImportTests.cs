using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class SpreadsheetImportTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    [Fact]
    public void Battle_line_with_quantity_and_pokerus_flag()
    {
        var preview = SpreadsheetImport.Battles("Caterpie;3;S", Data);

        var item = Assert.Single(preview.Items);
        Assert.Equal(("Caterpie", 3, 2), (item.Opponent, item.Count, item.Multiplier));
        Assert.Equal((1, 0), (preview.Recognized, preview.Ignored));
    }

    [Fact]
    public void Battle_lines_with_tabs_aliases_defaults_and_ignored_rows()
    {
        const string pasted = "data\tadversário\tqtd\r\n12/03\tBulbasaur (rival)\t\n\tWeedle\tx\tN\nnada aqui\n\nAbra\t2\ts\n";

        var preview = SpreadsheetImport.Battles(pasted, Data);

        Assert.Equal(
            [("Bulbasaur (rival)", 1, 1), ("Weedle", 1, 1), ("Abra", 2, 2)],
            preview.Items.Select(e => (e.Opponent, e.Count, e.Multiplier)));
        Assert.Equal((3, 2), (preview.Recognized, preview.Ignored));
    }

    [Fact]
    public void Level_line_with_stats_and_evs()
    {
        var preview = SpreadsheetImport.Levels("21\tWartortle\t62\t31\t42\t39\t44\t38\t38\t0\t2\t27\t0\t4", Pokemons.Squirtle());

        var level = Assert.Single(preview.Items);
        Assert.Equal((21, "Wartortle"), (level.Level, level.Species));
        Assert.Equal([62, 31, 42, 39, 44, 38], level.Stats);
        Assert.Equal([38, 0, 2, 27, 0, 4], level.Evs);
    }

    [Fact]
    public void Level_lines_missing_evs_become_zero_and_incomplete_rows_are_ignored()
    {
        const string pasted = "5;Squirtle;20;9;12;11;12;11\n13;Squirtle;37;18;24\n16;Charmander;1;1;1;1;1;1\nSquirtle;20;9;12;11;12;11";

        var preview = SpreadsheetImport.Levels(pasted, Pokemons.Squirtle());

        var level = Assert.Single(preview.Items);
        Assert.Equal(new int[6], level.Evs);
        Assert.Equal((1, 3), (preview.Recognized, preview.Ignored));
    }
}