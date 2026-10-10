using System.Globalization;

using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Evs;

namespace EvTrainer.Tests.Components.Evs;

public class EvEditorTests : AppTestContext
{
    private IRenderedComponent<EvSummary> OpenEditor()
    {
        Seed(Pokemons.Squirtle().WithEvents(("Caterpie", 10, 1), ("Geodude", 2, 1), ("Abra", 20, 1), ("Pidgey", 4, 1)));
        var cut = Render<EvSummary>();
        var edit = cut.Find("button.edit-evs");
        Assert.Equal(T("Evs.Edit"), edit.TextContent.Trim());
        edit.Click();
        return cut;
    }

    private static void Type(IRenderedComponent<EvSummary> cut, params int[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            cut.FindAll(".ev-editor .six input")[i].Input(values[i].ToString(CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public void Opens_prefilled_with_current_totals_and_keep_history_selected()
    {
        var cut = OpenEditor();

        var inputs = cut.FindAll(".ev-editor .six input");
        Assert.Equal(["10", "0", "2", "20", "0", "4"], inputs.Select(i => i.GetAttribute("value")));
        Assert.All(inputs, i => Assert.Equal("numeric", i.GetAttribute("inputmode")));
        Assert.True(cut.Find("input[type=radio][value=keep]").HasAttribute("checked"));
        Assert.Equal(T("EvEdit.Sum", 36), cut.Find(".ev-editor .ev-sum").TextContent.Trim());
    }

    [Fact]
    public void Sum_updates_live()
    {
        var cut = OpenEditor();

        Type(cut, 12, 0, 2, 26, 0, 4);

        Assert.Equal(T("EvEdit.Sum", 44), cut.Find(".ev-editor .ev-sum").TextContent.Trim());
    }

    [Theory]
    [InlineData("256")]
    [InlineData("-1")]
    [InlineData("x")]
    public void Invalid_values_disable_save(string value)
    {
        var cut = OpenEditor();

        cut.FindAll(".ev-editor .six input")[0].Input(value);

        Assert.True(cut.Find(".ev-editor button.save").HasAttribute("disabled"));
        Assert.Equal(T("EvEdit.Invalid"), cut.Find(".ev-editor .error").TextContent.Trim());
    }

    [Fact]
    public void Sum_above_510_disables_save()
    {
        var cut = OpenEditor();

        Type(cut, 255, 255, 2, 0, 0, 0);

        Assert.True(cut.Find(".ev-editor button.save").HasAttribute("disabled"));
        Assert.Equal(T("EvEdit.Invalid"), cut.Find(".ev-editor .error").TextContent.Trim());
    }

    [Fact]
    public void Keep_history_adds_an_adjustment_with_undo()
    {
        var cut = OpenEditor();

        Type(cut, 12, 0, 2, 26, 0, 4);
        cut.Find(".ev-editor button.save").Click();

        Assert.Equal([12, 0, 2, 26, 0, 4], Store.SelectedTotals);
        Assert.Equal(5, Repository.Stored["p1"].Events.Count);
        Assert.Equal(T("EvEdit.Adjusted"), Toasts.Message);
        Assert.Empty(cut.FindAll(".ev-editor"));

        cut.InvokeAsync(Toasts.RunUndo);

        Assert.Equal([10, 0, 2, 20, 0, 4], Store.SelectedTotals);
    }

    [Fact]
    public void Same_values_report_no_change()
    {
        var cut = OpenEditor();

        cut.Find(".ev-editor button.save").Click();

        Assert.Equal(T("EvEdit.NoChange"), Toasts.Message);
        Assert.Equal(4, Repository.Stored["p1"].Events.Count);
    }

    [Fact]
    public void Cancel_closes_without_changes()
    {
        var cut = OpenEditor();
        Type(cut, 12, 0, 2, 26, 0, 4);

        cut.Find(".ev-editor button.cancel").Click();

        Assert.Empty(cut.FindAll(".ev-editor"));
        Assert.Equal(4, Repository.Stored["p1"].Events.Count);
    }

    [Fact]
    public void Erase_history_asks_for_confirmation_and_cancel_changes_nothing()
    {
        var cut = OpenEditor();
        Type(cut, 12, 0, 2, 26, 0, 4);

        cut.Find("input[type=radio][value=erase]").Change("erase");
        cut.Find(".ev-editor button.save").Click();

        var dialog = cut.Find("[role=alertdialog]");
        Assert.Equal(T("EvEdit.Confirm.Message", 36), dialog.QuerySelector(".message")!.TextContent.Trim());
        Assert.Equal(T("EvEdit.Confirm.Erase", 36), dialog.QuerySelector("button.danger")!.TextContent.Trim());
        Assert.Equal(T("EvEdit.Confirm.Backup"), dialog.QuerySelector("button.backup")!.TextContent.Trim());
        Assert.Equal(4, Repository.Stored["p1"].Events.Count);

        dialog.QuerySelector("button.cancel")!.Click();

        Assert.Empty(cut.FindAll("[role=alertdialog]"));
        Assert.Equal(4, Repository.Stored["p1"].Events.Count);
        Assert.Equal([10, 0, 2, 20, 0, 4], Store.SelectedTotals);
    }

    [Fact]
    public void Confirmation_offers_a_backup_and_erasing_can_be_undone()
    {
        var cut = OpenEditor();
        Type(cut, 12, 0, 2, 26, 0, 4);
        cut.Find("input[type=radio][value=erase]").Change("erase");
        cut.Find(".ev-editor button.save").Click();

        cut.Find("[role=alertdialog] button.backup").Click();
        Assert.Single(Browser.Downloads);

        cut.Find("[role=alertdialog] button.danger").Click();

        Assert.Single(Repository.Stored["p1"].Events);
        Assert.Equal([12, 0, 2, 26, 0, 4], Store.SelectedTotals);
        Assert.Equal(T("EvEdit.HistoryErased", 36), Toasts.Message);
        Assert.Empty(cut.FindAll(".ev-editor"));

        cut.InvokeAsync(Toasts.RunUndo);

        Assert.Equal(4, Repository.Stored["p1"].Events.Count);
    }
}