using EvTrainer.Web.State;

namespace EvTrainer.Tests.Support;

/// <summary>
/// In-memory "Gist" shared by any number of simulated devices. Versions give ETags; failures can be queued;
/// <see cref="BeforeWrite"/> simulates another device writing between a read and a write.
/// </summary>
public sealed class FakeSyncProvider : ISyncProvider
{
    private readonly Queue<SyncErrorKind> _failures = new();
    private int _version;

    public const string Login = "treinador";
    public const string Location = "gist-1";

    public string? Json { get; private set; }

    public string ETag => $"\"v{_version}\"";

    public int Reads { get; private set; }

    public int Writes { get; private set; }

    public List<string> TokensSeen { get; } = [];

    /// <summary>Runs once, right before the next write is stored.</summary>
    public Action? BeforeWrite { get; set; }

    /// <summary>Runs once, right after the next read (another device writing between a read and a write).</summary>
    public Action? AfterNextRead { get; set; }

    /// <summary>When set, reads wait for it (to prove cycles never overlap).</summary>
    public TaskCompletionSource? HoldReads { get; set; }

    /// <summary>The next provider call fails with this kind.</summary>
    public void FailNext(SyncErrorKind kind) => _failures.Enqueue(kind);

    /// <summary>Simulates another device storing a document.</summary>
    public void Store(string json)
    {
        Json = json;
        _version++;
    }

    public Task<SyncAccount> ConnectAsync(string token, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        TokensSeen.Add(token);
        return Task.FromResult(new SyncAccount(Login, Location));
    }

    public async Task<RemoteRead> ReadAsync(SyncConnection connection, string? etag, CancellationToken cancellationToken = default)
    {
        if (HoldReads is { } hold)
        {
            await hold.Task;
        }

        ThrowIfFailing();
        Reads++;
        TokensSeen.Add(connection.Token);
        var read = etag == ETag ? new RemoteRead(null, ETag, NotModified: true) : new RemoteRead(Json, ETag, NotModified: false);
        if (AfterNextRead is { } after)
        {
            AfterNextRead = null;
            after();
        }

        return read;
    }

    public Task<string?> WriteAsync(SyncConnection connection, string json, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();
        if (BeforeWrite is { } before)
        {
            BeforeWrite = null;
            before();
        }

        Writes++;
        TokensSeen.Add(connection.Token);
        Store(json);
        return Task.FromResult<string?>(ETag);
    }

    private void ThrowIfFailing()
    {
        if (_failures.TryDequeue(out var kind))
        {
            throw new SyncException(kind);
        }
    }
}