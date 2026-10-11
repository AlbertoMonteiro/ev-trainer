using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Pages;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

/// <summary>SC-005: once connected, the token appears nowhere but the device's own sync record.</summary>
public class SyncTokenLeakTests : AppTestContext
{
    private const string Token = "ghp_SENTINEL_never_show_me_0123456789";

    [Fact]
    public async Task The_token_never_leaks()
    {
        Seed(Pokemons.Squirtle());
        await Sync.ConnectAsync(Token);
        var cut = Render<Home>();

        // Every screen the player can reach.
        var screens = new List<string> { cut.Markup };
        for (var tab = 0; tab < 3; tab++)
        {
            cut.FindAll("[role=tab]")[tab].Click();
            screens.Add(cut.Markup);
        }

        cut.Find("header button.menu").Click();
        screens.Add(cut.Markup);
        cut.Find(".menu-panel button.sync").Click();
        screens.Add(cut.Markup);
        foreach (var kind in Enum.GetValues<SyncErrorKind>())
        {
            SyncProvider.FailNext(kind);
            await cut.InvokeAsync(Sync.SyncNowAsync);
            screens.Add(cut.Markup);
            screens.Add(new SyncException(kind).ToString());
        }

        Assert.All(screens, text => Assert.DoesNotContain(Token, text, StringComparison.Ordinal));
        Assert.DoesNotContain(Token, Store.ExportBackup(), StringComparison.Ordinal);
        Assert.DoesNotContain(Token, SyncProvider.Json!, StringComparison.Ordinal);
        Assert.DoesNotContain(Token, Repository.SyncConnection!.ToString(), StringComparison.Ordinal);
        Assert.Equal(Token, Repository.SyncConnection.Token);
    }
}