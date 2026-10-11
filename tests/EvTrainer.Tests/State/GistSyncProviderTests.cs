using System.Net;
using System.Text.Json.Nodes;

using EvTrainer.Tests.Support;
using EvTrainer.Web.State;

using static EvTrainer.Tests.Support.FakeHttpHandler;

namespace EvTrainer.Tests.State;

/// <summary>GitHub Gist provider against a fake HTTP handler (research R1–R3). No network.</summary>
public class GistSyncProviderTests
{
    private const string Token = "ghp_token_sentinela_123";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly SyncConnection Connection = new(SyncConnection.Gist, Token, "alberto", "g42", null, null);

    private static (GistSyncProvider Provider, FakeHttpHandler Handler) Create(Func<Request, HttpResponseMessage> respond)
    {
        var handler = new FakeHttpHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri(GistSyncProvider.ApiBase) };
        return (new GistSyncProvider(http), handler);
    }

    private static string GistList(params (string Id, string CreatedAt, string File)[] gists) =>
        new JsonArray([.. gists.Select(g => (JsonNode)new JsonObject
        {
            ["id"] = g.Id,
            ["created_at"] = g.CreatedAt,
            ["files"] = new JsonObject { [g.File] = new JsonObject { ["filename"] = g.File } },
        })]).ToJsonString();

    private static string Gist(string content, bool truncated = false, string file = GistSyncProvider.FileName) =>
        new JsonObject { ["id"] = "g42", ["files"] = new JsonObject { [file] = new JsonObject { ["content"] = content, ["truncated"] = truncated } } }
            .ToJsonString();

    [Fact]
    public async Task Every_call_sends_the_token_and_the_github_headers()
    {
        var (provider, handler) = Create(_ => Json(HttpStatusCode.OK, Gist("{}"), ("ETag", "\"e1\"")));

        await provider.ReadAsync(Connection, null, Ct);

        var request = Assert.Single(handler.Requests);
        Assert.Equal($"Bearer {Token}", request.Headers["Authorization"]);
        Assert.Equal("application/vnd.github+json", request.Headers["Accept"]);
        Assert.Equal("2022-11-28", request.Headers["X-GitHub-Api-Version"]);
    }

    [Fact]
    public async Task Connect_finds_the_oldest_gist_with_the_file_across_pages()
    {
        var (provider, handler) = Create(r => r.PathAndQuery switch
        {
            "/user" => Json(HttpStatusCode.OK, """{ "login": "alberto" }"""),
            "/gists?per_page=100&page=1" => Json(
                HttpStatusCode.OK,
                GistList(("other", "2026-01-01T00:00:00Z", "notes.md"), ("newer", "2026-10-01T00:00:00Z", GistSyncProvider.FileName)),
                ("Link", "<https://api.github.com/gists?per_page=100&page=2>; rel=\"next\"")),
            "/gists?per_page=100&page=2" => Json(HttpStatusCode.OK, GistList(("older", "2026-09-01T00:00:00Z", GistSyncProvider.FileName))),
            _ => Status(HttpStatusCode.NotFound),
        });

        var account = await provider.ConnectAsync(Token, Ct);

        Assert.Equal(new SyncAccount("alberto", "older"), account);
        Assert.DoesNotContain(handler.Requests, r => r.Method == HttpMethod.Post);
    }

    [Fact]
    public async Task Connect_creates_a_secret_gist_when_there_is_none()
    {
        var (provider, handler) = Create(r => (r.Method.Method, r.PathAndQuery) switch
        {
            ("GET", "/user") => Json(HttpStatusCode.OK, """{ "login": "alberto" }"""),
            ("GET", "/gists?per_page=100&page=1") => Json(HttpStatusCode.OK, "[]"),
            ("POST", "/gists") => Json(HttpStatusCode.Created, """{ "id": "new-gist" }"""),
            _ => Status(HttpStatusCode.NotFound),
        });

        var account = await provider.ConnectAsync(Token, Ct);

        Assert.Equal("new-gist", account.Location);
        var body = JsonNode.Parse(handler.Requests.Single(r => r.Method == HttpMethod.Post).Body!)!;
        Assert.False(body["public"]!.GetValue<bool>());
        Assert.Equal(GistSyncProvider.Description, body["description"]!.GetValue<string>());
        Assert.NotNull(body["files"]![GistSyncProvider.FileName]!["content"]);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, SyncErrorKind.InvalidToken)]
    [InlineData(HttpStatusCode.InternalServerError, SyncErrorKind.Unknown)]
    public async Task Connect_maps_user_errors(HttpStatusCode status, SyncErrorKind expected)
    {
        var (provider, _) = Create(_ => Status(status));

        var error = await Assert.ThrowsAsync<SyncException>(() => provider.ConnectAsync(Token, Ct));

        Assert.Equal(expected, error.Kind);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Missing_gist_permission_is_reported(HttpStatusCode status)
    {
        var (provider, _) = Create(r => r.PathAndQuery == "/user"
            ? Json(HttpStatusCode.OK, """{ "login": "alberto" }""")
            : Status(status));

        var error = await Assert.ThrowsAsync<SyncException>(() => provider.ConnectAsync(Token, Ct));

        Assert.Equal(SyncErrorKind.MissingGistPermission, error.Kind);
    }

    [Fact]
    public async Task Rate_limit_is_reported()
    {
        var (provider, _) = Create(_ => Status(HttpStatusCode.Forbidden, ("x-ratelimit-remaining", "0")));

        var error = await Assert.ThrowsAsync<SyncException>(() => provider.ReadAsync(Connection, null, Ct));

        Assert.Equal(SyncErrorKind.RateLimited, error.Kind);
    }

    [Fact]
    public async Task Network_failure_is_offline_and_never_carries_the_token()
    {
        var (provider, _) = Create(_ => throw new HttpRequestException($"failed with Authorization: Bearer {Token}"));

        var error = await Assert.ThrowsAsync<SyncException>(() => provider.ReadAsync(Connection, null, Ct));

        Assert.Equal(SyncErrorKind.Offline, error.Kind);
        Assert.DoesNotContain(Token, error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Read_returns_the_file_content_and_etag()
    {
        var (provider, handler) = Create(_ => Json(HttpStatusCode.OK, Gist("""{ "collection": "pokemon" }"""), ("ETag", "\"e1\"")));

        var read = await provider.ReadAsync(Connection, "\"e0\"", Ct);

        Assert.Equal("""{ "collection": "pokemon" }""", read.Json);
        Assert.Equal("\"e1\"", read.ETag);
        Assert.False(read.NotModified);
        Assert.Equal("/gists/g42", handler.Requests[0].PathAndQuery);
        Assert.Equal("\"e0\"", handler.Requests[0].Headers["If-None-Match"]);
    }

    [Fact]
    public async Task Read_not_modified()
    {
        var (provider, _) = Create(_ => Status(HttpStatusCode.NotModified, ("ETag", "\"e1\"")));

        var read = await provider.ReadAsync(Connection, "\"e1\"", Ct);

        Assert.True(read.NotModified);
        Assert.Null(read.Json);
    }

    [Theory]
    [InlineData(true, GistSyncProvider.FileName)]
    [InlineData(false, "other.json")]
    public async Task Truncated_or_missing_file_is_invalid(bool truncated, string file)
    {
        var (provider, _) = Create(_ => Json(HttpStatusCode.OK, Gist("{}", truncated, file)));

        var error = await Assert.ThrowsAsync<SyncException>(() => provider.ReadAsync(Connection, null, Ct));

        Assert.Equal(SyncErrorKind.InvalidRemote, error.Kind);
    }

    [Fact]
    public async Task Write_patches_only_the_file_and_returns_the_new_etag()
    {
        var (provider, handler) = Create(_ => Json(HttpStatusCode.OK, Gist("x"), ("ETag", "\"e2\"")));

        var etag = await provider.WriteAsync(Connection, """{ "collection": "pokemon" }""", Ct);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/gists/g42", request.PathAndQuery);
        var body = JsonNode.Parse(request.Body!)!;
        Assert.Equal("""{ "collection": "pokemon" }""", body["files"]![GistSyncProvider.FileName]!["content"]!.GetValue<string>());
        Assert.Null(body["public"]);
        Assert.Equal("\"e2\"", etag);
    }
}