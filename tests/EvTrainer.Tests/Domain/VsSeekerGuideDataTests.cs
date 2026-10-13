using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 013: integrity of the embedded VS Seeker data generated from the FireRed decompilation.</summary>
public class VsSeekerGuideDataTests
{
    private static readonly VsSeekerGuide Guide = VsSeekerGuide.LoadEmbedded();
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();
    private static readonly LocationNames Places = LocationNames.LoadEmbedded();

    [Fact]
    public void Has_the_kanto_route_trainers_with_unique_ids()
    {
        Assert.True(Guide.Trainers.Count >= 150);
        Assert.Equal(Guide.Trainers.Count, Guide.Trainers.Select(t => t.Id).Distinct().Count());
        Assert.All(Guide.Trainers, t =>
        {
            Assert.Matches(@"^Rota \d+$", t.Location);
            Assert.True(Places.HasTranslation(t.Location), t.Location);
            Assert.All(t.Access, a => Assert.Contains(a, new[] { "surf", "cyclingRoad", "victoryRoad" }));
        });
    }

    [Fact]
    public void Stages_are_ordered_and_teams_are_valid_pokedex_species()
    {
        Assert.All(Guide.Trainers, t =>
        {
            Assert.NotEmpty(t.Stages);
            Assert.Equal(0, t.Stages[0].Index);
            Assert.Equal(t.Stages.Select(s => s.Index).Order(), t.Stages.Select(s => s.Index));
            Assert.All(t.Stages, s =>
            {
                Assert.InRange(s.Index, 0, 5);
                Assert.InRange(s.Team.Count, 1, 6);
                Assert.All(s.Team, p =>
                {
                    Assert.NotNull(Dex.Find(p.Species));
                    Assert.InRange(p.Level, 1, 100);
                });
            });
        });
    }

    [Fact]
    public void Anchor_trainers_match_the_game()
    {
        var bernie = Guide.Trainers.Single(t => t.Id == "ENGINEER_BERNIE");
        Assert.Equal(("Engineer", "Bernie", "Rota 11", false), (bernie.TrainerClass, bernie.Name, bernie.Location, bernie.Double));
        Assert.Equal([0, 3], bernie.Stages.Select(s => s.Index));
        Assert.Equal([("Magnemite", 18), ("Magnemite", 18), ("Magneton", 18)], bernie.Stages[0].Team);
        Assert.Equal([("Magneton", 28), ("Magneton", 28), ("Magneton", 28)], bernie.Stages[1].Team);

        Assert.Equal([0], Guide.Trainers.Single(t => t.Id == "LASS_JULIA").Stages.Select(s => s.Index));
        Assert.True(Guide.Trainers.Single(t => t.Id == "TWINS_ELI_ANNE").Double);
        Assert.Contains("surf", Guide.Trainers.First(t => t.Location == "Rota 21").Access);

        var gia = Guide.Trainers.Single(t => t.Id == "YOUNG_COUPLE_GIA_JES");
        Assert.True(gia.Stages[^1].RequiresEliteFour);
        Assert.False(gia.Stages[1].RequiresEliteFour);
    }
}