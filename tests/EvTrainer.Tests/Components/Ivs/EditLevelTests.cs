using System.Globalization;

using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Ivs;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Components.Ivs;

/// <summary>Editing a level note in place through <see cref="IvTab"/> (table + form).</summary>
public class EditLevelTests : AppTestContext
{
    private IRenderedComponent<IvTab> RenderTab()
    {
        var pokemon = Pokemons.Squirtle("Timid").WithEvents(("Caterpie", 50, 1));
        pokemon.IvLog.Add(new LevelEntry { Level = 16, Species = "Wartortle", Stats = [49, 25, 33, 30, 35, 30], Evs = [21, 0, 2, 5, 0, 3] });
        pokemon.IvLog.Add(new LevelEntry { Level = 21, Species = "Wartortle", Stats = [62, 31, 42, 39, 44, 38], Evs = [38, 0, 2, 27, 0, 4] });
        Seed(pokemon);
        return Render<IvTab>();
    }

    private static string[] Values(IRenderedComponent<IvTab> cut, string selector) =>
        [.. cut.FindAll(selector).Select(i => i.GetAttribute("value") ?? "")];

    [Fact]
    public void Each_level_label_is_an_edit_button()
    {
        var cut = RenderTab();

        var buttons = cut.FindAll(".iv-table tbody th button.edit");

        Assert.Equal(2, buttons.Count);
        Assert.Equal([T("Ivs.Row.Level", 16), T("Ivs.Row.Level", 21)], buttons.Select(b => b.TextContent.Trim()));
        Assert.Equal(T("Ivs.EditLabel", 21), buttons[1].GetAttribute("aria-label"));
        Assert.Equal(T("Ivs.EditHint"), cut.Find(".edit-hint").TextContent.Trim());
        Assert.Equal(T("Ivs.Form.Title"), cut.Find(".level-form h2").TextContent.Trim());
        Assert.Empty(cut.FindAll(".level-form button.cancel-edit"));
    }

    [Fact]
    public void Edit_fills_the_form_with_the_note_including_its_own_evs()
    {
        var cut = RenderTab();

        cut.FindAll(".iv-table button.edit")[1].Click();

        Assert.Equal(T("Ivs.Form.EditTitle", 21), cut.Find(".level-form h2").TextContent.Trim());
        Assert.Equal("21", cut.Find("#level-number").GetAttribute("value"));
        Assert.Equal("Wartortle", cut.Find("select#level-species").GetAttribute("value"));
        Assert.Equal(["62", "31", "42", "39", "44", "38"], Values(cut, ".level-form .stats input"));
        Assert.Equal(["38", "0", "2", "27", "0", "4"], Values(cut, ".level-form .evs input"));
        Assert.Equal(T("Ivs.Form.SaveChanges"), cut.Find(".level-form button.save-level").TextContent.Trim());
        Assert.Equal(T("Ivs.Form.CancelEdit"), cut.Find(".level-form button.cancel-edit").TextContent.Trim());
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void Saving_replaces_the_note_with_undo()
    {
        var cut = RenderTab();
        cut.FindAll(".iv-table button.edit")[1].Click();

        cut.FindAll(".level-form .stats input")[0].Input(63.ToString(CultureInfo.InvariantCulture));
        cut.Find(".level-form button.save-level").Click();

        var log = Repository.Stored["p1"].IvLog;
        Assert.Equal([16, 21], log.Select(l => l.Level));
        Assert.Equal(63, log[1].Stats[0]);
        Assert.Equal([38, 0, 2, 27, 0, 4], log[1].Evs);
        Assert.Equal(T("Ivs.Updated", 21), Toasts.Message);
        Assert.Equal(T("Ivs.Form.Title"), cut.Find(".level-form h2").TextContent.Trim());
        Assert.Equal("", cut.Find("#level-number").GetAttribute("value") ?? "");

        cut.InvokeAsync(Toasts.RunUndo);

        Assert.Equal(62, Repository.Stored["p1"].IvLog[1].Stats[0]);
    }

    [Fact]
    public void Cancel_returns_to_a_new_note_without_changes()
    {
        var cut = RenderTab();
        cut.FindAll(".iv-table button.edit")[1].Click();
        cut.FindAll(".level-form .stats input")[0].Input("99");
        var saves = Repository.SaveCount;

        cut.Find(".level-form button.cancel-edit").Click();

        Assert.Equal(saves, Repository.SaveCount);
        Assert.Equal(62, Repository.Stored["p1"].IvLog[1].Stats[0]);
        Assert.Equal(T("Ivs.Form.Title"), cut.Find(".level-form h2").TextContent.Trim());
        Assert.Equal(["50", "0", "0", "0", "0", "0"], Values(cut, ".level-form .evs input"));
    }
}