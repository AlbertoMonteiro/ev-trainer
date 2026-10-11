using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

/// <summary>Sync service against an in-memory provider and a manual clock (research R6–R8). No network.</summary>
public class SyncServiceTests
{
    private const string Token = "ghp_token_sentinela_123";
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    /// <summary>One simulated device: its own storage, store and sync service, sharing the remote.</summary>
    private sealed class Device
    {
        public Device(FakeSyncProvider remote, ManualTimeProvider time, params (string Id, EvTrainer.Web.Domain.Model.TrainingPokemon Pokemon)[] seed)
        {
            Repository = new InMemoryRepository(seed);
            Store = new TrainerStore(Repository, Data, time);
            Store.Load();
            Sync = new SyncService(Store, Repository, remote, time);
        }

        public InMemoryRepository Repository { get; }

        public TrainerStore Store { get; }

        public SyncService Sync { get; }
    }

    private readonly ManualTimeProvider _time = new();
    private readonly FakeSyncProvider _remote = new();

    private Device NewDevice(params (string Id, EvTrainer.Web.Domain.Model.TrainingPokemon Pokemon)[] seed) => new(_remote, _time, seed);

    [Fact]
    public async Task Connect_saves_the_connection_and_uploads_local_data()
    {
        var device = NewDevice(("a", Pokemons.Squirtle()), ("b", Pokemons.Squirtle()));

        var error = await device.Sync.ConnectAsync($"  {Token}  ");

        Assert.Null(error);
        var connection = device.Repository.SyncConnection!;
        Assert.Equal((Token, FakeSyncProvider.Login, FakeSyncProvider.Location), (connection.Token, connection.Login, connection.Location));
        Assert.Equal(SyncPhase.Synced, device.Sync.Phase);
        Assert.Equal(_time.GetUtcNow(), device.Sync.LastSyncedAt);
        Assert.Equal(FakeSyncProvider.Login, device.Sync.Login);
        var remote = PokemonJson.DeserializeSnapshot(_remote.Json!)!;
        Assert.Equal(["a", "b"], remote.Documents.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(SyncErrorKind.InvalidToken)]
    [InlineData(SyncErrorKind.MissingGistPermission)]
    [InlineData(SyncErrorKind.Offline)]
    public async Task Failed_connect_reports_the_kind_and_saves_nothing(SyncErrorKind kind)
    {
        var device = NewDevice(("a", Pokemons.Squirtle()));
        _remote.FailNext(kind);

        var error = await device.Sync.ConnectAsync(Token);

        Assert.Equal(kind, error);
        Assert.Null(device.Repository.SyncConnection);
        Assert.Equal(SyncPhase.Off, device.Sync.Phase);
        Assert.Null(_remote.Json);
    }

    [Fact]
    public async Task Empty_token_is_invalid_without_calling_the_provider()
    {
        var device = NewDevice();

        Assert.Equal(SyncErrorKind.InvalidToken, await device.Sync.ConnectAsync("   "));
        Assert.Empty(_remote.TokensSeen);
    }

    [Fact]
    public async Task Disconnect_forgets_the_token_and_keeps_local_data()
    {
        var device = NewDevice(("a", Pokemons.Squirtle()));
        await device.Sync.ConnectAsync(Token);

        device.Sync.Disconnect();

        Assert.Null(device.Repository.SyncConnection);
        Assert.Equal(SyncPhase.Off, device.Sync.Phase);
        Assert.Null(device.Sync.Login);
        Assert.Single(device.Store.All);
        Assert.NotNull(_remote.Json);
    }

    [Fact]
    public async Task The_token_never_goes_into_the_backup_or_the_remote_content()
    {
        var device = NewDevice(("a", Pokemons.Squirtle()));

        await device.Sync.ConnectAsync(Token);

        Assert.DoesNotContain(Token, device.Store.ExportBackup(), StringComparison.Ordinal);
        Assert.DoesNotContain(Token, _remote.Json!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_saved_connection_is_restored()
    {
        var first = NewDevice(("a", Pokemons.Squirtle()));
        await first.Sync.ConnectAsync(Token);

        var again = new SyncService(first.Store, first.Repository, _remote, _time);

        Assert.Equal(FakeSyncProvider.Login, again.Login);
        Assert.Equal(SyncPhase.Synced, again.Phase);
    }

    // ---- US2: two devices sharing the same remote ----

    private async Task<(Device A, Device B)> TwoConnectedDevices()
    {
        var a = NewDevice(("a", Pokemons.Squirtle().WithEvents(("Caterpie", 1, 1))));
        await a.Sync.ConnectAsync(Token);
        var b = NewDevice();
        await b.Sync.ConnectAsync(Token);
        return (a, b);
    }

    [Fact]
    public async Task Second_device_receives_the_first_ones_pokemon_and_hears_about_it()
    {
        var a = NewDevice(("a", Pokemons.Squirtle()));
        await a.Sync.ConnectAsync(Token);
        var b = NewDevice();
        MergeResult? heard = null;
        b.Sync.Merged += m => heard = m;

        await b.Sync.ConnectAsync(Token);

        Assert.Equal(["a"], b.Store.All.Keys);
        Assert.Equal(1, heard?.Added);
    }

    [Fact]
    public async Task Changes_travel_both_ways()
    {
        var (a, b) = await TwoConnectedDevices();
        _time.Advance(TimeSpan.FromMinutes(1));

        a.Store.Select("a");
        a.Store.RecordBattle("Caterpie");
        await a.Sync.SyncNowAsync();
        await b.Sync.SyncNowAsync();
        var created = b.Store.AddPokemon();
        await b.Sync.SyncNowAsync();
        await a.Sync.SyncNowAsync();

        Assert.Equal(2, b.Store.All["a"].Events[0].Count);
        Assert.True(a.Store.All.ContainsKey(created));
    }

    [Fact]
    public async Task The_most_recent_change_wins_a_conflict()
    {
        var (a, b) = await TwoConnectedDevices();
        a.Store.Select("a");
        b.Store.Select("a");

        _time.Advance(TimeSpan.FromMinutes(1));
        a.Store.TogglePokerus();                 // older, not synced yet
        _time.Advance(TimeSpan.FromMinutes(1));
        b.Store.ToggleMacho();                   // newer
        await b.Sync.SyncNowAsync();
        await a.Sync.SyncNowAsync();

        Assert.True(a.Store.All["a"].MachoBrace);
        Assert.False(a.Store.All["a"].Pokerus);
        Assert.Equal(a.Store.All["a"].UpdatedAt, PokemonJson.DeserializeSnapshot(_remote.Json!)!.Documents["a"].UpdatedAt);
    }

    [Fact]
    public async Task Deletions_travel_and_undo_brings_the_pokemon_back()
    {
        var (a, b) = await TwoConnectedDevices();
        MergeResult? heard = null;
        a.Sync.Merged += m => heard = m;
        _time.Advance(TimeSpan.FromMinutes(1));

        var undo = b.Store.DeletePokemon("a")!;
        await b.Sync.SyncNowAsync();
        await a.Sync.SyncNowAsync();

        Assert.Empty(a.Store.All);
        Assert.Equal(1, heard?.Removed);

        _time.Advance(TimeSpan.FromMinutes(1));
        undo();
        await b.Sync.SyncNowAsync();
        await a.Sync.SyncNowAsync();

        Assert.True(a.Store.All.ContainsKey("a"));
    }

    [Fact]
    public async Task Offline_keeps_local_data_and_catches_up_later()
    {
        var (a, _) = await TwoConnectedDevices();
        a.Store.Select("a");
        _time.Advance(TimeSpan.FromMinutes(1));
        a.Store.RecordBattle("Caterpie");
        _remote.FailNext(SyncErrorKind.Offline);

        await a.Sync.SyncNowAsync();

        Assert.Equal(SyncPhase.Pending, a.Sync.Phase);
        Assert.Equal(SyncErrorKind.Offline, a.Sync.Error);
        Assert.Equal(2, a.Store.All["a"].Events[0].Count);

        await a.Sync.SyncNowAsync();

        Assert.Equal(SyncPhase.Synced, a.Sync.Phase);
        Assert.Equal(2, PokemonJson.DeserializeSnapshot(_remote.Json!)!.Documents["a"].Events[0].Count);
    }

    [Fact]
    public async Task Other_errors_keep_local_data_and_show_the_error()
    {
        var (a, _) = await TwoConnectedDevices();
        _remote.FailNext(SyncErrorKind.InvalidToken);

        await a.Sync.SyncNowAsync();

        Assert.Equal(SyncPhase.Error, a.Sync.Phase);
        Assert.Equal(SyncErrorKind.InvalidToken, a.Sync.Error);
        Assert.Single(a.Store.All);
    }

    [Fact]
    public async Task A_write_by_another_device_between_read_and_write_is_merged_not_lost()
    {
        var (a, b) = await TwoConnectedDevices();
        _time.Advance(TimeSpan.FromMinutes(1));
        var fromB = b.Store.AddPokemon();
        a.Store.Select("a");
        a.Store.RecordBattle("Caterpie");
        _remote.AfterNextRead = () => b.Sync.SyncNowAsync().GetAwaiter().GetResult();

        await a.Sync.SyncNowAsync();

        var remote = PokemonJson.DeserializeSnapshot(_remote.Json!)!;
        Assert.True(remote.Documents.ContainsKey(fromB));
        Assert.Equal(2, remote.Documents["a"].Events[0].Count);
        Assert.True(a.Store.All.ContainsKey(fromB));
    }

    [Fact]
    public async Task Nothing_new_from_the_remote_raises_no_merge_notice()
    {
        var (a, _) = await TwoConnectedDevices();
        var heard = 0;
        a.Sync.Merged += _ => heard++;

        await a.Sync.SyncNowAsync();

        Assert.Equal(0, heard);
    }

    // ---- US2: automatic triggers ----

    [Fact]
    public async Task Start_syncs_when_connected_and_does_nothing_otherwise()
    {
        var off = NewDevice(("a", Pokemons.Squirtle()));
        await off.Sync.StartAsync();
        Assert.Equal(0, _remote.Reads);

        var (a, b) = await TwoConnectedDevices();
        _time.Advance(TimeSpan.FromMinutes(1));
        b.Store.AddPokemon();
        await b.Sync.SyncNowAsync();
        var restarted = new SyncService(a.Store, a.Repository, _remote, _time);

        await restarted.StartAsync();

        Assert.Equal(2, a.Store.All.Count);
    }

    [Fact]
    public async Task Coming_back_to_the_app_syncs()
    {
        var (a, b) = await TwoConnectedDevices();
        _time.Advance(TimeSpan.FromMinutes(1));
        b.Store.AddPokemon();
        await b.Sync.SyncNowAsync();

        await a.Sync.OnVisibleAsync();

        Assert.Equal(2, a.Store.All.Count);
    }

    [Fact]
    public async Task A_burst_of_changes_syncs_once_five_seconds_after_the_last()
    {
        var (a, _) = await TwoConnectedDevices();
        a.Store.Select("a");
        var writes = _remote.Writes;

        for (var i = 0; i < 3; i++)
        {
            a.Store.RecordBattle("Caterpie");
            _time.Advance(TimeSpan.FromSeconds(2));
        }

        Assert.Equal(SyncPhase.Pending, a.Sync.Phase);
        Assert.Equal(writes, _remote.Writes);

        _time.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal(writes + 1, _remote.Writes);
        Assert.Equal(SyncPhase.Synced, a.Sync.Phase);
        Assert.Equal(4, PokemonJson.DeserializeSnapshot(_remote.Json!)!.Documents["a"].Events[0].Count);
    }

    [Fact]
    public async Task Changes_applied_by_a_sync_do_not_trigger_another_sync()
    {
        var (a, b) = await TwoConnectedDevices();
        _time.Advance(TimeSpan.FromMinutes(1));
        b.Store.AddPokemon();
        await b.Sync.SyncNowAsync();
        _time.Advance(TimeSpan.FromSeconds(10));
        await a.Sync.SyncNowAsync();
        var reads = _remote.Reads;

        _time.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(reads, _remote.Reads);
    }

    [Fact]
    public async Task Cycles_never_run_at_the_same_time()
    {
        var (a, _) = await TwoConnectedDevices();
        var reads = _remote.Reads;
        var hold = new TaskCompletionSource();
        _remote.HoldReads = hold;

        var first = a.Sync.SyncNowAsync();
        var second = a.Sync.SyncNowAsync();
        Assert.Equal(reads, _remote.Reads);
        Assert.Equal(SyncPhase.Syncing, a.Sync.Phase);

        _remote.HoldReads = null;
        hold.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(SyncPhase.Synced, a.Sync.Phase);
    }

    [Fact]
    public async Task Nothing_runs_after_disconnecting()
    {
        var (a, _) = await TwoConnectedDevices();
        a.Sync.Disconnect();
        a.Store.Select("a");
        var reads = _remote.Reads;

        a.Store.RecordBattle("Caterpie");
        _time.Advance(TimeSpan.FromSeconds(10));
        await a.Sync.OnVisibleAsync();
        await a.Sync.SyncNowAsync();

        Assert.Equal(reads, _remote.Reads);
        Assert.Equal(SyncPhase.Off, a.Sync.Phase);
    }
}