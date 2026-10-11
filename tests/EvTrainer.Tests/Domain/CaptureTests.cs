using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class CaptureTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    /// <summary>Reference case: Squirtle Nv 5 Timid 20/9/12/11/12/11.</summary>
    private static readonly CaptureInput Squirtle = new("Squirtle", 5, "Timid", [20, 9, 12, 11, 12, 11]);

    private static string Show(IvCell cell) => IvTests.Show(cell);

    private static CaptureResult Evaluate(CaptureInput input, bool[]? counted = null) =>
        Capture.Evaluate(input, counted ?? Capture.DefaultCounted(Dex.Find(input.Species)!.Base), Dex, Data);

    [Theory]
    [InlineData(0, IvGrade.Poor)]
    [InlineData(10, IvGrade.Poor)]
    [InlineData(11, IvGrade.Fair)]
    [InlineData(19, IvGrade.Fair)]
    [InlineData(20, IvGrade.Good)]
    [InlineData(25, IvGrade.Good)]
    [InlineData(26, IvGrade.Great)]
    [InlineData(31, IvGrade.Great)]
    public void Grade_boundaries(int iv, IvGrade expected) => Assert.Equal(expected, Capture.Grade(iv));

    [Fact]
    public void Default_counted_ignores_the_weaker_attack_stat()
    {
        Assert.Equal([true, false, true, true, true, true], Capture.DefaultCounted([25, 20, 15, 105, 55, 90]));
        Assert.Equal([true, true, true, false, true, true], Capture.DefaultCounted([70, 80, 50, 35, 35, 35]));
        Assert.Equal([true, true, true, true, true, true], Capture.DefaultCounted([50, 60, 50, 60, 50, 50]));
    }

    [Fact]
    public void Input_validation()
    {
        Assert.True(Squirtle.IsValid(Dex, Data));
        Assert.False((Squirtle with { Species = "Agumon" }).IsValid(Dex, Data));
        Assert.False((Squirtle with { Level = 0 }).IsValid(Dex, Data));
        Assert.False((Squirtle with { Level = 101 }).IsValid(Dex, Data));
        Assert.False((Squirtle with { Nature = "Brave-ish" }).IsValid(Dex, Data));
        Assert.False((Squirtle with { Stats = [20, 9, 12, 11, 12, 0] }).IsValid(Dex, Data));
        Assert.False((Squirtle with { Stats = [20, 9, 12, 11, 12, 1000] }).IsValid(Dex, Data));
        Assert.False((Squirtle with { Stats = [20, 9, 12, 11, 12] }).IsValid(Dex, Data));
    }

    [Fact]
    public void Reference_case_matches_the_iv_table_and_is_uncertain()
    {
        var result = Evaluate(Squirtle);

        Assert.Equal(["12-31", "4-31", "10-29", "20-31", "12-31", "14-31"], result.Cells.Select(Show));
        Assert.Equal(new GradeSpan(IvGrade.Poor, IvGrade.Great), result.Grades[1]);
        Assert.Equal(new GradeSpan(IvGrade.Good, IvGrade.Great), result.Grades[3]);
        Assert.Equal(new VerdictUncertain(VerdictGrade.Fair, VerdictGrade.Excellent), result.Verdict);
        Assert.Equal(NatureEffect.Helps, result.NatureEffect);
        // HP, DEF, SPA, SPD, SPE count: (12+31)+(10+29)+(20+31)+(12+31)+(14+31).
        Assert.Equal(221, result.Score);
    }

    [Fact]
    public void Narrow_ranges_give_a_known_verdict()
    {
        // Level 50 Abra, Timid, every IV 30, EVs zero ⇒ every range is 30–31.
        var result = Evaluate(new CaptureInput("Abra", 50, "Timid", [100, 36, 35, 125, 75, 121]));

        Assert.All(result.Cells, c => Assert.Equal("30-31", Show(c)));
        Assert.Equal(new VerdictKnown(VerdictGrade.Excellent), result.Verdict);
    }

    [Fact]
    public void Impossible_counted_stat_makes_the_verdict_unavailable()
    {
        var result = Evaluate(Squirtle with { Stats = [99, 9, 12, 11, 12, 11] });

        Assert.Equal("?", Show(result.Cells[0]));
        Assert.Null(result.Grades[0]);
        Assert.Equal(new VerdictUnavailable(VerdictProblem.Inconsistent), result.Verdict);
        Assert.Null(result.Score);
    }

    [Fact]
    public void Impossible_stat_that_does_not_count_is_ignored()
    {
        var result = Evaluate(Squirtle with { Stats = [20, 99, 12, 11, 12, 11] });

        Assert.IsType<VerdictUncertain>(result.Verdict);
        Assert.NotNull(result.Score);
    }

    [Fact]
    public void Nothing_counted_makes_the_verdict_unavailable()
    {
        var result = Evaluate(Squirtle, new bool[6]);

        Assert.Equal(new VerdictUnavailable(VerdictProblem.NothingCounted), result.Verdict);
        Assert.Null(result.Score);
    }

    [Fact]
    public void Shedinja_hp_never_counts()
    {
        var result = Evaluate(new CaptureInput("Shedinja", 25, "Hardy", [1, 54, 33, 26, 26, 30]), [true, true, true, true, true, true]);

        Assert.IsType<IvUnknowable>(result.Cells[0]);
        Assert.Null(result.Grades[0]);
        Assert.IsNotType<VerdictUnavailable>(result.Verdict);
    }

    [Theory]
    [InlineData("Hardy", NatureEffect.Neutral)]
    [InlineData("Timid", NatureEffect.Helps)]
    [InlineData("Modest", NatureEffect.Helps)]
    [InlineData("Adamant", NatureEffect.Harms)]
    [InlineData("Lonely", NatureEffect.Harms)]
    public void Nature_effect_for_a_special_attacker(string nature, NatureEffect expected) =>
        Assert.Equal(expected, Capture.EffectOf(Data.FindNature(nature)!, [true, false, true, true, true, true]));

    [Fact]
    public void Nature_that_harms_a_physical_attacker()
    {
        var counted = new[] { true, true, true, false, true, true };

        Assert.Equal(NatureEffect.Harms, Capture.EffectOf(Data.FindNature("Modest")!, counted));
        Assert.Equal(NatureEffect.Neutral, Capture.EffectOf(Data.FindNature("Hardy")!, counted));
        Assert.Equal(NatureEffect.Neutral, Capture.EffectOf(Data.FindNature("Modest")!, [true, false, true, false, true, true]));
    }

    private static CaptureResult Scored(int score, NatureEffect nature) =>
        new([], [], new VerdictKnown(VerdictGrade.Good), nature, score);

    [Theory]
    [InlineData(104, 100, CaptureComparison.Equivalent)]
    [InlineData(100, 104, CaptureComparison.Equivalent)]
    [InlineData(105, 100, CaptureComparison.Better)]
    [InlineData(100, 105, CaptureComparison.Worse)]
    public void Compare_uses_the_score_with_a_two_point_margin(int current, int best, CaptureComparison expected) =>
        Assert.Equal(expected, Capture.Compare(Scored(current, NatureEffect.Neutral), Scored(best, NatureEffect.Neutral)));

    [Fact]
    public void Compare_breaks_ties_with_the_nature()
    {
        Assert.Equal(CaptureComparison.Better, Capture.Compare(Scored(100, NatureEffect.Helps), Scored(102, NatureEffect.Neutral)));
        Assert.Equal(CaptureComparison.Worse, Capture.Compare(Scored(102, NatureEffect.Neutral), Scored(100, NatureEffect.Helps)));
        Assert.Equal(CaptureComparison.Better, Capture.Compare(Scored(100, NatureEffect.Neutral), Scored(100, NatureEffect.Harms)));
        Assert.Equal(CaptureComparison.Equivalent, Capture.Compare(Scored(101, NatureEffect.Helps), Scored(100, NatureEffect.Helps)));
        // Beyond the margin the score wins over the nature.
        Assert.Equal(CaptureComparison.Worse, Capture.Compare(Scored(100, NatureEffect.Helps), Scored(105, NatureEffect.Harms)));
    }
}