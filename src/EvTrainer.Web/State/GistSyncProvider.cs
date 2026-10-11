using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EvTrainer.Web.State;

/// <summary>
/// Stores the data in a secret Gist of the user's own GitHub account, calling the GitHub REST API straight from
/// the browser (research R1–R3). Errors become <see cref="SyncException"/> with a kind only.
/// </summary>
public sealed class GistSyncProvider(HttpClient http) : ISyncProvider
{
    public const string ApiBase = "https://api.github.com/";
    public const string FileName = "evs-trainer.json";
    public const string Description = "EVs Trainer";

    private const string EmptyDocument = """{"collection":"pokemon","documents":{}}""";

    public async Task<SyncAccount> ConnectAsync(string token, CancellationToken cancellationToken = default)
    {
        string login;
        using (var user = await SendAsync(HttpMethod.Get, "user", token, null, null, gistCall: false, cancellationToken))
        using (var account = await ReadJsonAsync(user, cancellationToken))
        {
            login = account.RootElement.TryGetProperty("login", out var value) ? value.GetString() ?? "" : "";
        }

        return new SyncAccount(login, await FindGistAsync(token, cancellationToken) ?? await CreateGistAsync(token, cancellationToken));
    }

    public async Task<RemoteRead> ReadAsync(SyncConnection connection, string? etag, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"gists/{connection.Location}", connection.Token, null, etag, gistCall: true, cancellationToken);
        var newTag = response.Headers.ETag?.ToString();
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            return new RemoteRead(null, newTag ?? etag, NotModified: true);
        }

        using var gist = await ReadJsonAsync(response, cancellationToken);
        if (!gist.RootElement.TryGetProperty("files", out var files)
            || !files.TryGetProperty(FileName, out var file)
            || (file.TryGetProperty("truncated", out var truncated) && truncated.ValueKind == JsonValueKind.True)
            || !file.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.String)
        {
            throw new SyncException(SyncErrorKind.InvalidRemote);
        }

        return new RemoteRead(content.GetString(), newTag, NotModified: false);
    }

    public async Task<string?> WriteAsync(SyncConnection connection, string json, CancellationToken cancellationToken = default)
    {
        var body = Body(writer =>
        {
            writer.WriteStartObject("files");
            writer.WriteStartObject(FileName);
            writer.WriteString("content", json);
            writer.WriteEndObject();
            writer.WriteEndObject();
        });
        using var response = await SendAsync(HttpMethod.Patch, $"gists/{connection.Location}", connection.Token, body, null, gistCall: true, cancellationToken);
        return response.Headers.ETag?.ToString();
    }

    /// <summary>The oldest gist of the account that holds <see cref="FileName"/>, across all pages.</summary>
    private async Task<string?> FindGistAsync(string token, CancellationToken cancellationToken)
    {
        (string Id, DateTimeOffset CreatedAt)? oldest = null;
        for (var page = 1; ; page++)
        {
            using var response = await SendAsync(HttpMethod.Get, $"gists?per_page=100&page={page}", token, null, null, gistCall: true, cancellationToken);
            using var list = await ReadJsonAsync(response, cancellationToken);
            foreach (var gist in list.RootElement.EnumerateArray())
            {
                if (gist.TryGetProperty("files", out var files) && files.TryGetProperty(FileName, out _)
                    && gist.GetProperty("id").GetString() is { } id
                    && DateTimeOffset.TryParse(gist.GetProperty("created_at").GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
                    && (oldest is null || createdAt < oldest.Value.CreatedAt))
                {
                    oldest = (id, createdAt);
                }
            }

            var link = response.Headers.TryGetValues("Link", out var values) ? string.Join(",", values) : "";
            if (!link.Contains("rel=\"next\"", StringComparison.Ordinal))
            {
                return oldest?.Id;
            }
        }
    }

    private async Task<string> CreateGistAsync(string token, CancellationToken cancellationToken)
    {
        var body = Body(writer =>
        {
            writer.WriteString("description", Description);
            writer.WriteBoolean("public", false);
            writer.WriteStartObject("files");
            writer.WriteStartObject(FileName);
            writer.WriteString("content", EmptyDocument);
            writer.WriteEndObject();
            writer.WriteEndObject();
        });
        using var response = await SendAsync(HttpMethod.Post, "gists", token, body, null, gistCall: true, cancellationToken);
        using var gist = await ReadJsonAsync(response, cancellationToken);
        return gist.RootElement.GetProperty("id").GetString() ?? throw new SyncException(SyncErrorKind.Unknown);
    }

    /// <param name="gistCall">403/404 on gist endpoints mean the token cannot use gists.</param>
    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, string token, string? json, string? etag, bool gistCall, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        if (etag is not null)
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        }

        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            // The browser hides the details of a failed fetch; never pass its text on (it could echo headers).
            throw new SyncException(SyncErrorKind.Offline);
        }

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotModified)
        {
            return response;
        }

        var kind = KindOf(response, gistCall);
        response.Dispose();
        throw new SyncException(kind);
    }

    private static SyncErrorKind KindOf(HttpResponseMessage response, bool gistCall) => response.StatusCode switch
    {
        HttpStatusCode.Unauthorized => SyncErrorKind.InvalidToken,
        HttpStatusCode.TooManyRequests => SyncErrorKind.RateLimited,
        HttpStatusCode.Forbidden when response.Headers.TryGetValues("x-ratelimit-remaining", out var remaining) && remaining.Contains("0") =>
            SyncErrorKind.RateLimited,
        HttpStatusCode.Forbidden or HttpStatusCode.NotFound when gistCall => SyncErrorKind.MissingGistPermission,
        _ => SyncErrorKind.Unknown,
    };

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            throw new SyncException(SyncErrorKind.InvalidRemote);
        }
    }

    private static string Body(Action<Utf8JsonWriter> write)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            write(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}