using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Shell;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.Components.Shell;

public class ToastTests : AppTestContext
{
    [Fact]
    public void Shows_message_with_undo_and_runs_it()
    {
        var undone = false;
        var cut = Render<Toast>();

        cut.InvokeAsync(() => Toasts.Show("Caterpie: +1 HP", () => undone = true));

        Assert.Equal("Caterpie: +1 HP", cut.Find(".toast .msg").TextContent);
        cut.Find(".toast button").Click();
        Assert.True(undone);
        Assert.Empty(cut.FindAll(".toast"));
    }

    [Fact]
    public void Without_undo_there_is_no_button()
    {
        var cut = Render<Toast>();

        cut.InvokeAsync(() => Toasts.Show("Salvo"));

        Assert.Empty(cut.FindAll(".toast button"));
    }

    [Fact]
    public void Undo_button_uses_localized_text()
    {
        var cut = Render<Toast>();

        cut.InvokeAsync(() => Toasts.Show("x", () => { }));

        Assert.Equal(T("Toast.Undo"), cut.Find(".toast button").TextContent.Trim());
    }

    [Fact]
    public void Hides_after_four_seconds()
    {
        var cut = Render<Toast>();
        cut.InvokeAsync(() => Toasts.Show("x"));

        cut.InvokeAsync(() => Time.Advance(ToastService.Duration - TimeSpan.FromMilliseconds(1)));
        Assert.NotEmpty(cut.FindAll(".toast"));

        cut.InvokeAsync(() => Time.Advance(TimeSpan.FromMilliseconds(1)));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast")));
    }
}