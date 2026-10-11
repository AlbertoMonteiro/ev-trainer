using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Settings;

namespace EvTrainer.Tests.Components.Settings;

public class DeletePanelTests : AppTestContext
{
    [Fact]
    public void Deleting_removes_and_undo_restores_order()
    {
        var first = Pokemons.Squirtle();
        first.Order = 1;
        var second = Pokemons.Pidgey();
        second.Order = 2;
        Repository.Stored["a"] = first;
        Repository.Stored["b"] = second;
        Store.Load();
        var cut = Render<DeletePanel>();

        Assert.Equal(T("Delete.Title"), cut.Find("details summary").TextContent.Trim());
        cut.Find("button.danger").Click();

        Assert.Equal(["b"], Repository.Stored.Keys);
        Assert.Equal("b", Store.SelectedId);
        Assert.Equal(T("Toast.PokemonDeleted", "Squirtle"), Toasts.Message);

        cut.InvokeAsync(Toasts.RunUndo);

        Assert.Equal(["a", "b"], Store.Ordered.Select(o => o.Id));
        Assert.Equal(1, Repository.Stored["a"].Order);
    }
}