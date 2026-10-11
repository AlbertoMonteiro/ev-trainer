using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvTrainer.Web.State;

/// <summary>
/// The device's link to the user's external storage (spec 008), kept only on this device
/// (<c>evtrainer:v1:sync</c>), never in the backup or the synced content.
/// </summary>
/// <param name="Location">Where the data lives at the provider (the gist id).</param>
public sealed record SyncConnection(
    string Provider,
    string Token,
    string Login,
    string Location,
    DateTimeOffset? LastSyncedAt,
    string? ETag)
{
    public const string Gist = "gist";

    /// <summary>Never print the token (logs, debugging): only the provider and the account.</summary>
    public override string ToString() => $"{Provider}:{Login}";
}

/// <summary>Tolerant JSON of <see cref="SyncConnection"/>: anything incomplete or unknown means "not connected".</summary>
public static class SyncConnectionJson
{
    public static string Serialize(SyncConnection connection) => JsonSerializer.Serialize(
        new SyncConnectionDto
        {
            Provider = connection.Provider,
            Token = connection.Token,
            Login = connection.Login,
            Location = connection.Location,
            LastSyncedAt = connection.LastSyncedAt,
            ETag = connection.ETag,
        },
        SyncJsonContext.Default.SyncConnectionDto);

    public static SyncConnection? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, SyncJsonContext.Default.SyncConnectionDto) is
            {
                Provider: SyncConnection.Gist, Token: { Length: > 0 } token, Login: { Length: > 0 } login, Location: { Length: > 0 } location,
            } dto
                ? new SyncConnection(SyncConnection.Gist, token, login, location, dto.LastSyncedAt, dto.ETag)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal sealed class SyncConnectionDto
{
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("lastSyncedAt")]
    public DateTimeOffset? LastSyncedAt { get; set; }

    [JsonPropertyName("etag")]
    public string? ETag { get; set; }
}

[JsonSerializable(typeof(SyncConnectionDto))]
internal sealed partial class SyncJsonContext : JsonSerializerContext;