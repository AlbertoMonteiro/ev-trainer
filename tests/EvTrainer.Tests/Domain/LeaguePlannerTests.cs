using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 015: effectiveness with abilities, coverage, threats and the move suggestion.</summary>
public class LeaguePlannerTests
{
    private static readonly League League = League.LoadEmbedded();
    private static readonly LeaguePlanner Planner = new(League, Pokedex.LoadEmbedded());

    private static LeaguePlan Plan(params PlanMember[] members) => new(LeagueRound.First, Starter.Bulbasaur, members, null);

    private static PlanMember Member(string species, params string[] moves) =>
        LeaguePlans.NewMember(species, League) with { Moves = moves };

    private static double? Hit(string move, string species, string? ability = null) =>
        Planner.Effectiveness(League.Move(move)!, League.Species(species)!.Types, ability ?? League.Species(species)!.Abilities[0]);

    [Theory]
    [InlineData("Earthquake", "Gengar", 0.0)]
    [InlineData("Thunderbolt", "Gyarados", 4.0)]
    [InlineData("Surf", "Lapras", 0.0)]
    [InlineData("Flamethrower", "Dewgong", 0.5)]
    [InlineData("Thunderbolt", "Lanturn", 0.0)]
    [InlineData("Flamethrower", "Ninetales", 0.0)]
    [InlineData("Thunderbolt", "Shedinja", 0.0)]
    [InlineData("Flamethrower", "Shedinja", 2.0)]
    [InlineData("Seismic Toss", "Gengar", 0.0)]
    [InlineData("Night Shade", "Snorlax", 0.0)]
    [InlineData("Dragon Rage", "Gyarados", 1.0)]
    [InlineData("Seismic Toss", "Machamp", 1.0)]
    [InlineData("Fissure", "Gengar", 0.0)]
    [InlineData("Return", "Gengar", 0.0)]
    [InlineData("Brick Break", "Snorlax", 2.0)]
    public void Effectiveness_uses_types_and_abilities(string move, string species, double expected) =>
        Assert.Equal(expected, Hit(move, species));

    [Fact]
    public void Status_moves_have_no_effectiveness() => Assert.Null(Hit("Hail", "Dewgong"));

    [Fact]
    public void The_chosen_ability_of_a_member_counts() =>
        Assert.Equal(0.0, Hit("Earthquake", "Gengar", "Levitate"));

    [Fact]
    public void Coverage_shows_the_best_move_of_the_team_against_each_opponent()
    {
        var coverage = Planner.Coverage(Plan(Member("Raichu", "Thunderbolt", "Thunder Wave"), Member("Golem", "Earthquake")));

        Assert.Equal(26, coverage.Count);
        var gyarados = coverage.First(c => c.Opponent.Trainer.Id == "lance" && c.Opponent.Mon.Species == "Gyarados");
        Assert.Equal(CoverageLevel.SuperEffective, gyarados.Level);
        Assert.Equal((0, "Thunderbolt", 4.0), (gyarados.Best!.Member, gyarados.Best.Move, gyarados.Best.Multiplier));

        var onix = coverage.First(c => c.Opponent.Mon.Species == "Onix");
        Assert.Equal((1, "Earthquake", 2.0), (onix.Best!.Member, onix.Best.Move, onix.Best.Multiplier));

        var gengar = coverage.First(c => c.Opponent.Trainer.Id == "agatha" && c.Opponent.Mon.Species == "Gengar");
        Assert.Equal(CoverageLevel.Neutral, gengar.Level);
        Assert.Equal("Thunderbolt", gengar.Best!.Move);

        var dragonair = coverage.First(c => c.Opponent.Mon.Species == "Dragonair");
        Assert.Equal(CoverageLevel.Neutral, dragonair.Level);
        Assert.Equal("Earthquake", dragonair.Best!.Move);
    }

    [Fact]
    public void Opponents_only_resisted_or_immune_are_marked()
    {
        var coverage = Planner.Coverage(Plan(Member("Raichu", "Thunderbolt")));

        Assert.Equal(CoverageLevel.None, coverage.First(c => c.Opponent.Mon.Species == "Onix").Level);
        Assert.Equal(CoverageLevel.Resisted, coverage.First(c => c.Opponent.Mon.Species == "Dragonair").Level);
        Assert.Equal(CoverageLevel.None, coverage.First(c => c.Opponent.Mon.Species == "Rhydon").Level);
    }

    [Fact]
    public void Summary_counts_super_effective_neutral_and_unanswered()
    {
        var empty = Planner.Summary(Planner.Coverage(Plan()));
        Assert.Equal(new CoverageSummary(0, 0, 26, 26), empty);

        var summary = Planner.Summary(Planner.Coverage(Plan(Member("Raichu", "Thunderbolt"))));
        Assert.Equal(26, summary.SuperEffective + summary.Neutral + summary.Unanswered);
        Assert.True(summary.SuperEffective > 0);
        Assert.True(summary.Unanswered > 0);
    }

    [Fact]
    public void Coverage_follows_the_round_and_the_starter()
    {
        var rematch = new LeaguePlan(LeagueRound.Rematch, Starter.Squirtle, [], null);
        var coverage = Planner.Coverage(rematch);
        Assert.Equal("Venusaur", coverage[^1].Opponent.Mon.Species);
        Assert.Contains(coverage, c => c.Opponent.Mon.Species == "Piloswine");
    }

    [Fact]
    public void Threats_are_damaging_moves_super_effective_against_a_member()
    {
        var threats = Planner.Threats(Plan(Member("Charizard")));

        Assert.Contains(threats, t => t.Opponent.Mon.Species == "Lapras" && t.Move == "Surf" && t.Multiplier == 2);
        Assert.Contains(threats, t => t.Opponent.Mon.Species == "Onix" && t.Move == "Rock Tomb" && t.Multiplier == 4);
        Assert.DoesNotContain(threats, t => t.Opponent.Mon.Species == "Lapras" && t.Move == "Ice Beam");
        Assert.DoesNotContain(threats, t => t.Opponent.Trainer.Id == "agatha" && t.Opponent.Index == 0);
        Assert.All(threats, t => Assert.NotEqual(MoveDamage.Status, League.Move(t.Move)!.Damage));
    }

    [Fact]
    public void Walls_are_opponents_whose_damaging_moves_all_are_resisted()
    {
        var walls = Planner.Walls(Plan(Member("Gengar")), 0);

        Assert.Contains(walls, o => o.Mon.Species == "Hitmonlee");
        Assert.DoesNotContain(walls, o => o.Mon.Species == "Hitmonchan");
        Assert.DoesNotContain(walls, o => o.Mon.Species == "Onix");
    }

    [Fact]
    public void Suggestion_fills_free_slots_with_learnable_damaging_moves()
    {
        var plan = Planner.Suggest(Plan(LeaguePlans.NewMember("Raichu", League)));

        var raichu = League.Species("Raichu")!;
        var moves = plan.Members[0].Moves;
        Assert.Equal(4, moves.Count);
        Assert.Equal(4, moves.Distinct().Count());
        Assert.All(moves, m => Assert.True(raichu.CanLearn(m), m));
        Assert.All(moves, m => Assert.True(League.Move(m)!.DealsDamage, m));
        Assert.Contains("Thunderbolt", moves);
        Assert.Equal(moves, Planner.Suggest(Plan(LeaguePlans.NewMember("Raichu", League))).Members[0].Moves);
    }

    [Fact]
    public void Suggestion_keeps_locked_moves()
    {
        var member = Member("Raichu", "Thunder Wave", "Slam") with { Locked = ["Thunder Wave"] };

        var moves = Planner.Suggest(Plan(LeaguePlans.Normalize(Plan(member), League).Members[0])).Members[0].Moves;

        Assert.Contains("Thunder Wave", moves);
        Assert.Equal(4, moves.Count);
    }

    [Fact]
    public void Suggestion_for_the_team_covers_what_one_member_alone_cannot()
    {
        var plan = Planner.Suggest(Plan(Member("Raichu", "Thunderbolt") with { Locked = ["Thunderbolt"] }, LeaguePlans.NewMember("Golem", League)));

        var coverage = Planner.Coverage(plan);
        Assert.All(coverage.Where(c => c.Opponent.Mon.Species == "Onix"), c => Assert.Equal(CoverageLevel.SuperEffective, c.Level));
        Assert.Equal(0, Planner.Summary(coverage).Unanswered);
    }

    [Fact]
    public void Suggestion_for_one_member_leaves_the_others_alone()
    {
        var plan = Plan(Member("Raichu"), LeaguePlans.NewMember("Golem", League));

        var suggested = Planner.Suggest(plan, member: 1);

        Assert.Empty(suggested.Members[0].Moves);
        Assert.Equal(4, suggested.Members[1].Moves.Count);
    }
}