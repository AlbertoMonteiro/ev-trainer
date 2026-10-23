using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 015, contracts/plan-sync.md: the planned team in the synced document; the latest change wins.</summary>
public class PlanSyncMergeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset At10 = Now.AddHours(-2);
    private static readonly DateTimeOffset At11 = Now.AddHours(-1);

    private static LeaguePlan Plan(DateTimeOffset at, string species = "Raichu", LeagueRound round = LeagueRound.First) =>
        new(round, Starter.Bulbasaur, [new PlanMember(species, "", [], [])], at);

    private static SyncSnapshot Snapshot(LeaguePlan? plan) =>
        new(new Dictionary<string, TrainingPokemon>(), new Dictionary<string, DateTimeOffset>(), plan);

    private static MergeResult Merge(LeaguePlan? local, LeaguePlan? remote) => SyncMerge.Merge(Snapshot(local), Snapshot(remote), Now);

    [Fact]
    public void Newer_remote_plan_replaces_the_local_one()
    {
        var result = Merge(Plan(At10), Plan(At11, "Golem"));

        Assert.Equal("Golem", result.Plan!.Members[0].Species);
        Assert.True(result.LocalChanged);
        Assert.True(result.PlanChanged);
        Assert.False(result.RemoteChanged);
    }

    [Fact]
    public void Newer_local_plan_is_uploaded()
    {
        var result = Merge(Plan(At11, "Golem"), Plan(At10));

        Assert.Equal("Golem", result.Plan!.Members[0].Species);
        Assert.False(result.LocalChanged);
        Assert.False(result.PlanChanged);
        Assert.True(result.RemoteChanged);
    }

    [Fact]
    public void A_plan_wins_over_no_plan_on_either_side()
    {
        var down = Merge(null, Plan(At10));
        Assert.Equal("Raichu", down.Plan!.Members[0].Species);
        Assert.True(down.LocalChanged);
        Assert.True(down.PlanChanged);

        var up = Merge(Plan(At10), null);
        Assert.True(up.RemoteChanged);
        Assert.False(up.LocalChanged);

        var none = Merge(null, null);
        Assert.Null(none.Plan);
        Assert.False(none.LocalChanged || none.RemoteChanged);
    }

    [Fact]
    public void Same_plan_changes_nothing()
    {
        var result = Merge(Plan(At10), Plan(At10));

        Assert.False(result.LocalChanged);
        Assert.False(result.RemoteChanged);
    }

    [Fact]
    public void A_tie_is_decided_by_the_content_so_both_sides_agree()
    {
        var a = Plan(At10, "Golem");
        var b = Plan(At10, "Raichu");

        Assert.Equal(Merge(a, b).Plan!.Members[0].Species, Merge(b, a).Plan!.Members[0].Species);
    }

    [Fact]
    public void The_synced_document_carries_the_plan_and_the_backup_does_not()
    {
        var plan = Plan(At10, "Golem", LeagueRound.Rematch);
        var json = PokemonJson.Serialize(new Dictionary<string, TrainingPokemon>(), indented: false, plan: plan);

        var snapshot = PokemonJson.DeserializeSnapshot(json)!;
        Assert.Equal(LeagueRound.Rematch, snapshot.Plan!.Round);
        Assert.Equal(At10, snapshot.Plan.UpdatedAt);
        Assert.Equal("Golem", snapshot.Plan.Members[0].Species);

        Assert.DoesNotContain("\"plan\"", PokemonJson.Serialize(new Dictionary<string, TrainingPokemon>(), indented: false));
        Assert.Null(PokemonJson.DeserializeSnapshot("""{"collection":"pokemon","documents":{}}""")!.Plan);
        Assert.Null(PokemonJson.DeserializeSnapshot("""{"collection":"pokemon","documents":{},"plan":5}""")!.Plan);
    }
}