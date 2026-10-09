using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Shell;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.Components.Shell;

public class StatusIndicatorTests : AppTestContext
{
    [Theory]
    [InlineData(SaveStatus.Saved, "📱", "Status.Saved")]
    [InlineData(SaveStatus.Saving, "🔄", "Status.Saving")]
    [InlineData(SaveStatus.Failed, "⚠️", "Status.Failed")]
    public void Shows_emoji_and_accessible_text(SaveStatus status, string emoji, string key)
    {
        var cut = Render<StatusIndicator>(p => p.Add(c => c.Status, status));

        var element = cut.Find("[role=status]");
        Assert.Equal(emoji, element.TextContent.Trim());
        Assert.Equal(T(key), element.GetAttribute("aria-label"));
        Assert.Equal(T(key), element.GetAttribute("title"));
    }

    [Fact]
    public void Failed_save_is_reflected_in_the_header()
    {
        Repository.FailSaves = true;
        Store.Load();
        var cut = Render<EvTrainer.Web.Pages.Home>();

        cut.Find(".empty button.primary").Click();

        Assert.Equal(T("Status.Failed"), cut.Find("header [role=status]").GetAttribute("aria-label"));
    }
}