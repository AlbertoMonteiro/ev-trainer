namespace EvTrainer.Web.State;

/// <summary>Why a sync step failed; the UI turns each one into a message (never the server's text).</summary>
public enum SyncErrorKind
{
    Offline,
    InvalidToken,
    MissingGistPermission,
    RateLimited,
    InvalidRemote,
    Unknown,
}

/// <summary>The account behind a token and where the data lives at the provider (the gist id).</summary>
public sealed record SyncAccount(string Login, string Location);

/// <param name="Json">The stored document; null when <paramref name="NotModified"/>.</param>
public sealed record RemoteRead(string? Json, string? ETag, bool NotModified);

/// <summary>
/// The user's external storage (constitution v1.3.0, Principle V). The first provider is a secret Gist; a Google
/// Drive provider can be added without touching the merge or the sync service.
/// </summary>
public interface ISyncProvider
{
    /// <summary>Validates the token and finds (or creates) the place that holds the data.</summary>
    Task<SyncAccount> ConnectAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Reads the stored document; with <paramref name="etag"/> an unchanged document is not downloaded.</summary>
    Task<RemoteRead> ReadAsync(SyncConnection connection, string? etag, CancellationToken cancellationToken = default);

    /// <summary>Stores the document and returns its new ETag, when the provider gives one.</summary>
    Task<string?> WriteAsync(SyncConnection connection, string json, CancellationToken cancellationToken = default);
}

/// <summary>A failed provider call. Carries only the kind, so no token or server text can leak into messages.</summary>
public sealed class SyncException(SyncErrorKind kind) : Exception($"Sync failed: {kind}")
{
    public SyncErrorKind Kind { get; } = kind;
}