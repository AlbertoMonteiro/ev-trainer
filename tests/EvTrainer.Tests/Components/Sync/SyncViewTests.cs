using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Pages;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.Components.Sync;

public class SyncViewTests : AppTestContext
{
    private const string Token = "ghp_token_sentinela_123";

    private IRenderedComponent<Home> OpenFromMenu()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();
        var item = cut.Find(".menu-panel button.sync");
        Assert.Equal(T("Menu.Sync"), item.TextContent.Trim());
        item.Click();
        return cut;
    }

    private static void Connect(IRenderedComponent<Home> cut, string token)
    {
        cut.Find(".sync-view input#sync-token").Input(token);
        cut.Find(".sync-view button.connect").Click();
    }

    [Fact]
    public void Off_explains_warns_and_shows_the_steps()
    {
        var cut = OpenFromMenu();

        Assert.Equal(T("Sync.Title"), cut.Find(".sync-view h2").TextContent.Trim());
        Assert.Equal(T("Sync.Intro"), cut.Find(".sync-view .intro").TextContent.Trim());
        Assert.Equal(T("Sync.SecretWarning"), cut.Find(".sync-view .secret-warning").TextContent.Trim());
        Assert.Equal(4, cut.FindAll(".sync-view ol.steps > li").Count);
        Assert.StartsWith("https://github.com/settings/personal-access-tokens/new", cut.Find(".sync-view a.token-link").GetAttribute("href"));
        Assert.Contains("scopes=gist", cut.Find(".sync-view a.classic-link").GetAttribute("href"));
        Assert.Equal("_blank", cut.Find(".sync-view a.token-link").GetAttribute("target"));
        var input = cut.Find(".sync-view input#sync-token");
        Assert.Equal("password", input.GetAttribute("type"));
        Assert.Equal("off", input.GetAttribute("autocomplete"));
        Assert.True(cut.Find(".sync-view button.connect").HasAttribute("disabled"));
    }

    [Fact]
    public void Connecting_shows_the_account_and_never_the_token()
    {
        var cut = OpenFromMenu();

        Connect(cut, Token);

        cut.WaitForAssertion(() => Assert.Equal(T("Sync.ConnectedAs", FakeSyncProvider.Login), cut.Find(".sync-view .connected").TextContent.Trim()));
        Assert.DoesNotContain(Token, cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".sync-view input#sync-token"));
        Assert.NotNull(cut.Find(".sync-view button.sync-now"));
        Assert.NotNull(cut.Find(".sync-view .last-sync"));
    }

    [Theory]
    [InlineData(SyncErrorKind.InvalidToken)]
    [InlineData(SyncErrorKind.MissingGistPermission)]
    [InlineData(SyncErrorKind.Offline)]
    [InlineData(SyncErrorKind.RateLimited)]
    public void Connection_errors_are_explained(SyncErrorKind kind)
    {
        var cut = OpenFromMenu();
        SyncProvider.FailNext(kind);

        Connect(cut, Token);

        cut.WaitForAssertion(() => Assert.Equal(T($"Sync.Error.{kind}"), cut.Find(".sync-view .error").TextContent.Trim()));
        Assert.NotEmpty(cut.FindAll(".sync-view input#sync-token"));
        Assert.Null(Repository.SyncConnection);
    }

    [Fact]
    public void Disconnect_asks_first_and_goes_back_to_off()
    {
        var cut = OpenFromMenu();
        Connect(cut, Token);
        cut.WaitForAssertion(() => cut.Find(".sync-view button.disconnect"));

        cut.Find(".sync-view button.disconnect").Click();
        var dialog = cut.Find(".sync-view [role=alertdialog]");
        Assert.Equal(T("Sync.Disconnect.Confirm"), dialog.QuerySelector(".message")!.TextContent.Trim());
        dialog.QuerySelector("button.cancel")!.Click();
        Assert.NotNull(Repository.SyncConnection);

        cut.Find(".sync-view button.disconnect").Click();
        cut.Find(".sync-view [role=alertdialog] button.danger").Click();

        Assert.Null(Repository.SyncConnection);
        Assert.NotEmpty(cut.FindAll(".sync-view input#sync-token"));
    }

    [Fact]
    public void Close_returns_to_the_tabs()
    {
        var cut = OpenFromMenu();

        cut.Find(".sync-view button.close-sync").Click();

        Assert.Empty(cut.FindAll(".sync-view"));
        Assert.NotEmpty(cut.FindAll("[role=tablist]"));
    }
}