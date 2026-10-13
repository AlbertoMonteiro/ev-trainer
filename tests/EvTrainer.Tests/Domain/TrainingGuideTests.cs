using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 013: ranking the VS Seeker trainers by the EVs they give.</summary>
public class TrainingGuideTests
{
    private const int Hp = 0, Attack = 1, SpAttack = 3, Speed = 5;

    private static readonly TrainingGuide Guide = new(VsSeekerGuide.LoadEmbedded(), Pokedex.LoadEmbedded());

    private static GuideTrainer Trainer(string id) => VsSeekerGuide.LoadEmbedded().Trainers.Single(t => t.Id == id);

    [Fact]
    public void Stage_evs_are_the_sum_of_the_team_yields()
    {
        var bernie = Trainer("ENGINEER_BERNIE");

        Assert.Equal([0, 0, 0, 4, 0, 0], Guide.StageEvs(bernie.Stages[0]));
        Assert.Equal([0, 0, 0, 6, 0, 0], Guide.StageEvs(bernie.Stages[1]));
    }

    [Fact]
    public void Sp_attack_before_the_elite_four_starts_with_bernie()
    {
        var first = Guide.Rank(SpAttack, "before")[0];

        Assert.Equal("ENGINEER_BERNIE", first.Trainer.Id);
        Assert.Equal(6, first.Score);
        Assert.Equal(3, first.Best.Index);
    }

    [Fact]
    public void Each_trainer_is_its_own_entry()
    {
        var hp = Guide.Rank(Hp, "before");

        Assert.Equal(4, hp.Single(e => e.Trainer.Id == "LASS_JULIA").Score);
        Assert.Equal(4, hp.Single(e => e.Trainer.Id == "TWINS_ELI_ANNE").Score);
        Assert.Equal(hp.Count, hp.Select(e => e.Trainer.Id).Distinct().Count());
    }

    [Theory]
    [InlineData(Hp)]
    [InlineData(Attack)]
    [InlineData(SpAttack)]
    [InlineData(Speed)]
    public void Only_trainers_giving_the_stat_are_listed_highest_first(int stat)
    {
        var entries = Guide.Rank(stat, "before");

        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.True(e.Score > 0 && Guide.StageEvs(e.Best)[stat] == e.Score));
        Assert.Equal(entries.Select(e => e.Score).OrderDescending(), entries.Select(e => e.Score));
    }

    [Fact]
    public void Ties_go_to_the_lower_level_team()
    {
        var entries = Guide.Rank(Hp, "before");
        for (var i = 1; i < entries.Count; i++)
        {
            if (entries[i].Score == entries[i - 1].Score)
            {
                Assert.True(entries[i - 1].Best.Team.Max(p => p.Level) <= entries[i].Best.Team.Max(p => p.Level));
            }
        }
    }

    [Fact]
    public void Before_the_elite_four_hides_stages_4_and_5()
    {
        var gia = Guide.Rank(Hp, "before").Single(e => e.Trainer.Id == "YOUNG_COUPLE_GIA_JES");

        Assert.Equal([0, 3], gia.VisibleStages.Select(s => s.Index));
        Assert.Equal(3, gia.Best.Index);
        Assert.Equal(2, gia.Score);
    }

    [Fact]
    public void After_the_elite_four_shows_every_stage_and_picks_the_best()
    {
        var gia = Guide.Rank(Hp, "after").Single(e => e.Trainer.Id == "YOUNG_COUPLE_GIA_JES");

        Assert.Equal([0, 3, 5], gia.VisibleStages.Select(s => s.Index));
        Assert.Equal(5, gia.Best.Index);
        Assert.Equal(3, gia.Score);
    }

    [Theory]
    [InlineData(new[] { 0, 0, 0, 0, 0, 0 }, new[] { 0, 0, 0, 252, 0, 0 }, SpAttack)]
    [InlineData(new[] { 52, 0, 0, 0, 0, 200 }, new[] { 252, 0, 0, 0, 0, 252 }, Hp)]
    [InlineData(new[] { 0, 0, 0, 0, 0, 0 }, new[] { 0, 0, 252, 0, 252, 0 }, Hp)]
    [InlineData(new[] { 10, 0, 0, 0, 0, 0 }, new[] { 10, 100, 0, 0, 0, 0 }, Attack)]
    public void Initial_stat_is_the_offered_stat_missing_the_most(int[] totals, int[] targets, int expected) =>
        Assert.Equal(expected, TrainingGuide.InitialStat(totals, targets));

    [Fact]
    public void Wasted_stats_are_those_the_stage_gives_whose_target_is_already_met()
    {
        var grace = Trainer("BEAUTY_GRACE").Stages[0];
        var evs = Guide.StageEvs(grace);
        Assert.True(evs[Hp] > 0 && evs[Speed] > 0);

        Assert.Equal([Speed], Guide.WastedStats(grace, totals: [0, 0, 0, 0, 0, 20], targets: [252, 0, 0, 0, 0, 20]));
        Assert.Equal([Speed], Guide.WastedStats(grace, totals: [0, 0, 0, 0, 0, 0], targets: [252, 0, 0, 0, 0, 0]));
        Assert.Empty(Guide.WastedStats(grace, totals: [0, 0, 0, 0, 0, 0], targets: [252, 0, 0, 0, 0, 252]));
    }
}