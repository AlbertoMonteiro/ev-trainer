using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

/// <summary>Spec 015: the planned team in the store, saved on the device and stamped for the sync.</summary>
public class TrainerStorePlanTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly League League = League.LoadEmbedded();

    private readonly InMemoryRepository _repository = new();
    private readonly ManualTimeProvider _time = new();

    private TrainerStore Load()
    {
        var store = new TrainerStore(_repository, Data, _time, League);
        store.Load();
        return store;
    }

    [Fact]
    public void Starts_with_an_empty_unstamped_plan()
    {
        var store = Load();

        Assert.Equal((LeagueRound.First, Starter.Bulbasaur, null), (store.Plan.Round, store.Plan.Starter, store.Plan.UpdatedAt));
        Assert.Empty(store.Plan.Members);
        Assert.Null(store.Snapshot().Plan);
    }

    [Fact]
    public void Update_stamps_saves_and_notifies()
    {
        var store = Load();
        var changed = 0;
        var local = 0;
        store.Changed += () => changed++;
        store.LocalDataChanged += () => local++;

        store.UpdatePlan(store.Plan with { Round = LeagueRound.Rematch, Members = [LeaguePlans.NewMember("Raichu", League)] });

        Assert.Equal(LeagueRound.Rematch, store.Plan.Round);
        Assert.Equal(_time.GetUtcNow(), store.Plan.UpdatedAt);
        Assert.Equal((1, 1), (changed, local));
        Assert.Equal("Raichu", _repository.Plan!.Members[0].Species);
        Assert.Equal(store.Plan, store.Snapshot().Plan);
    }

    [Fact]
    public void Update_drops_what_the_species_cannot_learn()
    {
        var store = Load();

        store.UpdatePlan(store.Plan with { Members = [new PlanMember("Raichu", "", ["Thunderbolt", "Surf"], [])] });

        Assert.Equal(["Thunderbolt"], store.Plan.Members[0].Moves);
        Assert.Equal("Static", store.Plan.Members[0].Ability);
    }

    [Fact]
    public void Load_reads_the_saved_plan()
    {
        _repository.SavePlan(new LeaguePlan(LeagueRound.Rematch, Starter.Squirtle, [new PlanMember("Golem", "Rock Head", ["Earthquake"], [])], _time.GetUtcNow()));

        var store = Load();

        Assert.Equal(Starter.Squirtle, store.Plan.Starter);
        Assert.Equal("Golem", store.Plan.Members[0].Species);
    }

    [Fact]
    public void Applying_a_sync_replaces_the_plan_without_stamping_or_raising_local_changes()
    {
        var store = Load();
        var local = 0;
        store.LocalDataChanged += () => local++;
        var remote = new LeaguePlan(LeagueRound.First, Starter.Charmander, [new PlanMember("Golem", "", ["Earthquake"], [])], _time.GetUtcNow().AddHours(-1));

        store.ApplySync(new MergeResult([], [], 0, 0, 0, true, false, remote, PlanChanged: true));

        Assert.Equal(Starter.Charmander, store.Plan.Starter);
        Assert.Equal(remote.UpdatedAt, store.Plan.UpdatedAt);
        Assert.Equal("Rock Head", store.Plan.Members[0].Ability);
        Assert.Equal(Starter.Charmander, _repository.Plan!.Starter);
        Assert.Equal(0, local);
    }
}

/// <summary>Spec 015: the planned team travels between devices through the sync.</summary>
public class PlanSyncServiceTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly League League = League.LoadEmbedded();

    private readonly ManualTimeProvider _time = new();
    private readonly FakeSyncProvider _remote = new();

    private (TrainerStore Store, SyncService Sync) NewDevice()
    {
        var repository = new InMemoryRepository();
        var store = new TrainerStore(repository, Data, _time, League);
        store.Load();
        return (store, new SyncService(store, repository, _remote, _time));
    }

    [Fact]
    public async Task A_plan_changed_on_one_device_reaches_the_other()
    {
        var phone = NewDevice();
        var pc = NewDevice();
        await phone.Sync.ConnectAsync("token");
        await pc.Sync.ConnectAsync("token");

        phone.Store.UpdatePlan(phone.Store.Plan with { Starter = Starter.Squirtle, Members = [LeaguePlans.NewMember("Lapras", League)] });
        await phone.Sync.SyncNowAsync();
        _time.Advance(TimeSpan.FromMinutes(1));
        await pc.Sync.SyncNowAsync();

        Assert.Equal(Starter.Squirtle, pc.Store.Plan.Starter);
        Assert.Equal("Lapras", pc.Store.Plan.Members[0].Species);
        Assert.Equal("Lapras", PokemonJson.DeserializeSnapshot(_remote.Json!)!.Plan!.Members[0].Species);
    }

    [Fact]
    public async Task Changing_the_plan_schedules_a_sync()
    {
        var device = NewDevice();
        await device.Sync.ConnectAsync("token");

        device.Store.UpdatePlan(device.Store.Plan with { Round = LeagueRound.Rematch });

        Assert.Equal(SyncPhase.Pending, device.Sync.Phase);
        _time.Advance(SyncService.Debounce);
        await Task.Yield();
        Assert.Equal(LeagueRound.Rematch, PokemonJson.DeserializeSnapshot(_remote.Json!)!.Plan!.Round);
    }
}