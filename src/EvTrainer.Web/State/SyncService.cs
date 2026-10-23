using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;

using Microsoft.JSInterop;

namespace EvTrainer.Web.State;

public enum SyncPhase
{
    Off,
    Syncing,
    Synced,
    Pending,
    Error,
}

/// <summary>
/// Keeps the local data and the user's external copy in step (spec 008, research R6–R8): one cycle at a time,
/// merging per Pokémon with <see cref="SyncMerge"/>; local data is never lost when the network or the provider fails.
/// Holds no UI text.
/// </summary>
public sealed class SyncService : IDisposable
{
    private const int MaxAttempts = 3;

    /// <summary>Quiet time after the last local change before syncing (a burst of battles becomes one sync).</summary>
    public static readonly TimeSpan Debounce = TimeSpan.FromSeconds(5);

    private readonly TrainerStore _store;
    private readonly IPokemonRepository _repository;
    private readonly ISyncProvider _provider;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SyncConnection? _connection;
    private string? _remoteJson;
    private bool _applying;
    private bool _dirty;
    private ITimer? _debounce;

    public SyncService(TrainerStore store, IPokemonRepository repository, ISyncProvider provider, TimeProvider time)
    {
        _store = store;
        _repository = repository;
        _provider = provider;
        _time = time;
        _connection = repository.LoadSyncConnection();
        Phase = _connection is null ? SyncPhase.Off : _connection.LastSyncedAt is null ? SyncPhase.Pending : SyncPhase.Synced;
        _store.LocalDataChanged += OnLocalDataChanged;
    }

    public event Action? Changed;

    /// <summary>Raised when a sync brought changes from another device.</summary>
    public event Action<MergeResult>? Merged;

    public SyncPhase Phase { get; private set; }

    /// <summary>Why the last cycle failed (also set while pending because the device is offline).</summary>
    public SyncErrorKind? Error { get; private set; }

    public bool IsConnected => _connection is not null;

    public string? Login => _connection?.Login;

    public string? Location => _connection?.Location;

    public DateTimeOffset? LastSyncedAt => _connection?.LastSyncedAt;

    /// <summary>Validates the token, links this device to the user's storage and syncs; returns why it failed.</summary>
    public async Task<SyncErrorKind?> ConnectAsync(string token)
    {
        token = token.Trim();
        if (token.Length == 0)
        {
            return SyncErrorKind.InvalidToken;
        }

        SyncAccount account;
        try
        {
            account = await _provider.ConnectAsync(token);
        }
        catch (SyncException error)
        {
            return error.Kind;
        }

        _connection = new SyncConnection(SyncConnection.Gist, token, account.Login, account.Location, null, null);
        _repository.SaveSyncConnection(_connection);
        _remoteJson = null;
        Error = null;
        await SyncNowAsync();
        return null;
    }

    /// <summary>Called once the app is up: syncs when this device is connected.</summary>
    public Task StartAsync() => SyncNowAsync();

    /// <summary>Called by the browser when the page becomes visible again (back to the app).</summary>
    [JSInvokable]
    public Task OnVisibleAsync() => SyncNowAsync();

    /// <summary>Forgets the token on this device; the local data and the remote copy stay as they are.</summary>
    public void Disconnect()
    {
        _debounce?.Dispose();
        _debounce = null;
        _connection = null;
        _remoteJson = null;
        _dirty = false;
        _repository.ClearSyncConnection();
        Error = null;
        SetPhase(SyncPhase.Off);
    }

    /// <summary>One sync cycle; never runs two at once.</summary>
    public async Task SyncNowAsync()
    {
        if (_connection is null)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (_connection is not { } connection)
            {
                return;
            }

            _dirty = false;
            SetPhase(SyncPhase.Syncing);
            var read = await ReadAsync(connection, connection.ETag);
            for (var attempt = 1; ; attempt++)
            {
                var remote = Parse(read.Json);
                var merge = SyncMerge.Merge(_store.Snapshot(), remote, _time.GetUtcNow());
                Apply(merge);
                if (!merge.RemoteChanged)
                {
                    connection = connection with { ETag = read.ETag };
                    break;
                }

                // The provider cannot write conditionally: check that nobody wrote since the read, else merge again.
                var check = await ReadAsync(connection, read.ETag);
                if (!check.NotModified && check.ETag != read.ETag && attempt < MaxAttempts)
                {
                    read = check;
                    continue;
                }

                var json = PokemonJson.Serialize(merge.Documents, indented: false, merge.Deleted, merge.Plan);
                var etag = await _provider.WriteAsync(connection, json);
                _remoteJson = json;
                connection = connection with { ETag = etag };
                break;
            }

            _connection = connection with { LastSyncedAt = _time.GetUtcNow() };
            _repository.SaveSyncConnection(_connection);
            Error = null;
            SetPhase(_dirty ? SyncPhase.Pending : SyncPhase.Synced);
        }
        catch (SyncException error) when (_connection is not null)
        {
            Error = error.Kind;
            SetPhase(error.Kind is SyncErrorKind.Offline ? SyncPhase.Pending : SyncPhase.Error);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _store.LocalDataChanged -= OnLocalDataChanged;
        _debounce?.Dispose();
        _gate.Dispose();
    }

    /// <summary>Reads the remote copy; an unchanged copy comes from memory (re-downloaded after a restart).</summary>
    private async Task<RemoteRead> ReadAsync(SyncConnection connection, string? etag)
    {
        var read = await _provider.ReadAsync(connection, _remoteJson is null ? null : etag);
        if (!read.NotModified)
        {
            _remoteJson = read.Json;
            return read;
        }

        return read with { Json = _remoteJson };
    }

    /// <summary>An empty or unreadable remote copy counts as empty: the merge then rewrites it from local data.</summary>
    private static SyncSnapshot Parse(string? json) =>
        (json is null ? null : PokemonJson.DeserializeSnapshot(json))
        ?? new SyncSnapshot(new Dictionary<string, Domain.Model.TrainingPokemon>(), new Dictionary<string, DateTimeOffset>());

    private void Apply(MergeResult merge)
    {
        if (!merge.LocalChanged)
        {
            return;
        }

        _applying = true;
        try
        {
            _store.ApplySync(merge);
        }
        finally
        {
            _applying = false;
        }

        if (merge.Updated + merge.Added + merge.Removed > 0)
        {
            Merged?.Invoke(merge);
        }
    }

    private void OnLocalDataChanged()
    {
        if (_connection is null || _applying)
        {
            return;
        }

        _dirty = true;
        if (Phase is not SyncPhase.Syncing)
        {
            SetPhase(SyncPhase.Pending);
        }

        _debounce?.Dispose();
        _debounce = _time.CreateTimer(_ => _ = SyncNowAsync(), null, Debounce, Timeout.InfiniteTimeSpan);
    }

    private void SetPhase(SyncPhase phase)
    {
        Phase = phase;
        Changed?.Invoke();
    }
}