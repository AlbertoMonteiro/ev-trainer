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

    // ---- Sync status (spec 008, US3) ----

    private const string Token = "ghp_token_sentinela_123";

    private static string Label(IRenderedComponent<StatusIndicator> cut) => cut.Find("[role=status]").GetAttribute("aria-label")!;

    [Fact]
    public async Task Connected_and_synced_shows_the_cloud_with_the_time()
    {
        Seed(Pokemons.Squirtle());
        await Sync.ConnectAsync(Token);

        var cut = Render<StatusIndicator>(p => p.Add(c => c.Status, SaveStatus.Saved));

        Assert.Equal("☁️", cut.Find("[role=status]").TextContent.Trim());
        Assert.Equal(T("Sync.Status.Synced", TimeZoneInfo.ConvertTime(Sync.LastSyncedAt!.Value, Time.LocalTimeZone)), Label(cut));
    }

    [Fact]
    public async Task Pending_changes_are_shown()
    {
        Seed(Pokemons.Squirtle());
        await Sync.ConnectAsync(Token);
        var cut = Render<StatusIndicator>(p => p.Add(c => c.Status, SaveStatus.Saved));

        Store.TogglePokerus();

        Assert.Equal("⏳", cut.Find("[role=status]").TextContent.Trim());
        Assert.Equal(T("Sync.Status.Pending"), Label(cut));
    }

    [Fact]
    public async Task Syncing_is_shown()
    {
        Seed(Pokemons.Squirtle());
        await Sync.ConnectAsync(Token);
        var cut = Render<StatusIndicator>(p => p.Add(c => c.Status, SaveStatus.Saved));
        var hold = new TaskCompletionSource();
        SyncProvider.HoldReads = hold;

        var running = cut.InvokeAsync(Sync.SyncNowAsync);

        Assert.Equal("🔄", cut.Find("[role=status]").TextContent.Trim());
        Assert.Equal(T("Sync.Status.Syncing"), Label(cut));
        SyncProvider.HoldReads = null;
        hold.SetResult();
        await running;
    }

    [Theory]
    [InlineData(SyncErrorKind.InvalidToken)]
    [InlineData(SyncErrorKind.RateLimited)]
    public async Task Errors_are_shown(SyncErrorKind kind)
    {
        Seed(Pokemons.Squirtle());
        await Sync.ConnectAsync(Token);
        var cut = Render<StatusIndicator>(p => p.Add(c => c.Status, SaveStatus.Saved));
        SyncProvider.FailNext(kind);

        await cut.InvokeAsync(Sync.SyncNowAsync);

        Assert.Equal("⚠️", cut.Find("[role=status]").TextContent.Trim());
        Assert.Equal(T($"Sync.Status.Error.{kind}"), Label(cut));
    }

    [Fact]
    public async Task A_local_save_failure_wins_over_the_sync_state()
    {
        Seed(Pokemons.Squirtle());
        await Sync.ConnectAsync(Token);

        var cut = Render<StatusIndicator>(p => p.Add(c => c.Status, SaveStatus.Failed));

        Assert.Equal(T("Status.Failed"), Label(cut));
    }

    [Fact]
    public void Tapping_the_indicator_opens_the_sync_screen()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<EvTrainer.Web.Pages.Home>();

        cut.Find("header button.status-button").Click();

        Assert.NotEmpty(cut.FindAll(".sync-view"));
    }

    [Fact]
    public async Task A_token_error_offers_to_reconnect()
    {
        Seed(Pokemons.Squirtle());
        await Sync.ConnectAsync(Token);
        SyncProvider.FailNext(SyncErrorKind.InvalidToken);
        await Sync.SyncNowAsync();
        var cut = Render<EvTrainer.Web.Pages.Home>();
        cut.Find("header button.status-button").Click();

        Assert.Equal(T("Sync.Error.InvalidToken"), cut.Find(".sync-view .error").TextContent.Trim());
        cut.Find(".sync-view button.reconnect").Click();

        Assert.NotEmpty(cut.FindAll(".sync-view input#sync-token"));
        Assert.False(Sync.IsConnected);
        Assert.Single(Store.All);
    }

    [Fact]
    public async Task Changes_from_another_device_are_announced()
    {
        Seed(Pokemons.Squirtle(), "mine");
        var other = new Dictionary<string, EvTrainer.Web.Domain.Model.TrainingPokemon> { ["theirs"] = Pokemons.Squirtle() };
        SyncProvider.Store(EvTrainer.Web.Domain.Json.PokemonJson.Serialize(other, indented: false));
        var cut = Render<EvTrainer.Web.Pages.Home>();

        await cut.InvokeAsync(() => Sync.ConnectAsync(Token));

        Assert.Equal(T("Sync.Merged", 0, 1, 0), Toasts.Message);
    }
}