using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 015: the planned team on the device and in the synced document (contracts/plan-sync.md).</summary>
public class LeaguePlanJsonTests
{
    private static readonly League League = League.LoadEmbedded();

    [Fact]
    public void Round_trips_every_field()
    {
        var plan = new LeaguePlan(
            LeagueRound.Rematch,
            Starter.Squirtle,
            [new PlanMember("Raichu", "Static", ["Thunderbolt", "Dig"], ["Dig"])],
            new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero));

        var read = LeaguePlanJson.Deserialize(LeaguePlanJson.Serialize(plan));

        Assert.NotNull(read);
        Assert.Equal(LeagueRound.Rematch, read.Round);
        Assert.Equal(Starter.Squirtle, read.Starter);
        Assert.Equal(plan.UpdatedAt, read.UpdatedAt);
        var member = Assert.Single(read.Members);
        Assert.Equal(("Raichu", "Static"), (member.Species, member.Ability));
        Assert.Equal(["Thunderbolt", "Dig"], member.Moves);
        Assert.Equal(["Dig"], member.Locked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    public void Missing_or_invalid_json_is_no_plan(string? json) => Assert.Null(LeaguePlanJson.Deserialize(json));

    [Fact]
    public void Reading_is_tolerant_and_normalizes_values()
    {
        var read = LeaguePlanJson.Deserialize("""
            {"round":"x","starter":"pikachu","members":[
              {"species":"Raichu","ability":"Static","moves":["Thunderbolt","Thunderbolt","Surf","Dig","Slam","Thunder"],"locked":["Surf","Fly"]},
              null,
              {"species":"Gengar"}
            ]}
            """);

        Assert.NotNull(read);
        Assert.Equal(LeagueRound.First, read.Round);
        Assert.Equal(Starter.Bulbasaur, read.Starter);
        Assert.Null(read.UpdatedAt);
        Assert.Equal(2, read.Members.Count);
        Assert.Equal(["Thunderbolt", "Surf", "Dig", "Slam"], read.Members[0].Moves);
        Assert.Equal(["Surf"], read.Members[0].Locked);
        Assert.Equal(("Gengar", ""), (read.Members[1].Species, read.Members[1].Ability));
        Assert.Empty(read.Members[1].Moves);
    }

    [Fact]
    public void Keeps_at_most_six_members()
    {
        var members = string.Join(",", Enumerable.Repeat("""{"species":"Pidgey"}""", 8));
        Assert.Equal(6, LeaguePlanJson.Deserialize($$"""{"members":[{{members}}]}""")!.Members.Count);
    }

    [Fact]
    public void Normalize_drops_unknown_species_and_unlearnable_moves_and_fixes_the_ability()
    {
        var plan = new LeaguePlan(LeagueRound.First, Starter.Bulbasaur,
        [
            new PlanMember("Raichu", "Levitate", ["Thunderbolt", "Flamethrower", "Dig"], ["Flamethrower", "Dig"]),
            new PlanMember("Missingno", "", ["Surf"], []),
            new PlanMember("Gengar", "", [], []),
        ], null);

        var normalized = LeaguePlans.Normalize(plan, League);

        Assert.Equal(["Raichu", "Gengar"], normalized.Members.Select(m => m.Species));
        Assert.Equal("Static", normalized.Members[0].Ability);
        Assert.Equal(["Thunderbolt", "Dig"], normalized.Members[0].Moves);
        Assert.Equal(["Dig"], normalized.Members[0].Locked);
        Assert.Equal("Levitate", normalized.Members[1].Ability);
    }

    [Fact]
    public void Changing_the_species_keeps_only_the_moves_the_new_one_learns()
    {
        var member = new PlanMember("Pikachu", "Static", ["Thunderbolt", "Thunder Wave", "Surf"], ["Surf"]);

        var changed = LeaguePlans.WithSpecies(member, "Gengar", League);

        Assert.Equal("Gengar", changed.Species);
        Assert.Equal("Levitate", changed.Ability);
        Assert.Equal(["Thunderbolt"], changed.Moves);
        Assert.Empty(changed.Locked);
    }
}

/// <summary>Spec 015: editing the planned team.</summary>
public class LeaguePlansTests
{
    private static readonly League League = League.LoadEmbedded();

    private static PlanMember Raichu(params string[] moves) => new("Raichu", "Static", moves, []);

    [Fact]
    public void Setting_a_move_fills_or_replaces_a_slot_and_clearing_removes_it()
    {
        var member = LeaguePlans.WithMove(Raichu(), 0, "Thunderbolt");
        member = LeaguePlans.WithMove(member, 3, "Dig");
        Assert.Equal(["Thunderbolt", "Dig"], member.Moves);

        member = LeaguePlans.WithMove(member, 0, "Thunder");
        Assert.Equal(["Thunder", "Dig"], member.Moves);

        member = LeaguePlans.WithMove(member with { Locked = ["Thunder"] }, 0, null);
        Assert.Equal(["Dig"], member.Moves);
        Assert.Empty(member.Locked);
    }

    [Fact]
    public void A_move_already_in_another_slot_moves_there()
    {
        var member = LeaguePlans.WithMove(Raichu("Thunderbolt", "Dig"), 1, "Thunderbolt");
        Assert.Equal(["Thunderbolt"], member.Moves);
    }

    [Fact]
    public void Toggling_a_lock_needs_the_move()
    {
        var member = LeaguePlans.ToggleLock(Raichu("Thunderbolt"), "Thunderbolt");
        Assert.Equal(["Thunderbolt"], member.Locked);
        Assert.Empty(LeaguePlans.ToggleLock(member, "Thunderbolt").Locked);
        Assert.Empty(LeaguePlans.ToggleLock(Raichu(), "Dig").Locked);
    }

    [Fact]
    public void Members_are_added_replaced_and_removed_up_to_six()
    {
        var plan = LeaguePlan.Empty;
        for (var i = 0; i < 7; i++)
        {
            plan = LeaguePlans.Add(plan, LeaguePlans.NewMember("Pidgey", League));
        }

        Assert.Equal(6, plan.Members.Count);
        plan = LeaguePlans.Replace(plan, 1, LeaguePlans.NewMember("Golem", League));
        Assert.Equal("Golem", plan.Members[1].Species);
        plan = LeaguePlans.Remove(plan, 0);
        Assert.Equal("Golem", plan.Members[0].Species);
        Assert.Equal(5, plan.Members.Count);
    }
}