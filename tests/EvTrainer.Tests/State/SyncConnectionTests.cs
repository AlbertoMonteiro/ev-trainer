using EvTrainer.Tests.Support;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

public class SyncConnectionTests
{
    private static readonly SyncConnection Sample = new(
        "gist", "token-sentinela", "alberto", "abc123", new DateTimeOffset(2026, 10, 11, 21, 0, 0, TimeSpan.Zero), "\"etag-1\"");

    [Fact]
    public void Round_trips_through_the_repository()
    {
        var repository = new InMemoryRepository();

        repository.SaveSyncConnection(Sample);

        Assert.Equal(Sample, repository.LoadSyncConnection());
    }

    [Fact]
    public void Json_round_trips_and_invalid_documents_are_null()
    {
        Assert.Equal(Sample, SyncConnectionJson.Deserialize(SyncConnectionJson.Serialize(Sample)));
        Assert.Null(SyncConnectionJson.Deserialize(null));
        Assert.Null(SyncConnectionJson.Deserialize("not json"));
        Assert.Null(SyncConnectionJson.Deserialize("""{ "provider": "gist", "login": "a", "location": "b" }"""));
        Assert.Null(SyncConnectionJson.Deserialize("""{ "provider": "drive", "token": "t", "login": "a", "location": "b" }"""));
    }

    [Fact]
    public void Clear_removes_the_connection()
    {
        var repository = new InMemoryRepository();
        repository.SaveSyncConnection(Sample);

        repository.ClearSyncConnection();

        Assert.Null(repository.LoadSyncConnection());
    }
}