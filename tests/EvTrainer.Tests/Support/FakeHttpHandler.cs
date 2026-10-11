using System.Net;
using System.Text;

namespace EvTrainer.Tests.Support;

/// <summary>Answers HTTP requests from a function and records them (method, path, headers, body). No network.</summary>
public sealed class FakeHttpHandler(Func<FakeHttpHandler.Request, HttpResponseMessage> respond) : HttpMessageHandler
{
    public sealed record Request(HttpMethod Method, string PathAndQuery, IReadOnlyDictionary<string, string> Headers, string? Body);

    public List<Request> Requests { get; } = [];

    public static HttpResponseMessage Json(HttpStatusCode status, string json, params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        foreach (var (name, value) in headers)
        {
            response.Headers.TryAddWithoutValidation(name, value);
        }

        return response;
    }

    public static HttpResponseMessage Status(HttpStatusCode status, params (string Name, string Value)[] headers) =>
        Json(status, "{}", headers);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new Request(request.Method, request.RequestUri!.PathAndQuery, headers, body);
        Requests.Add(recorded);
        return respond(recorded);
    }
}