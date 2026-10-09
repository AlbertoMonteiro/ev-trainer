using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class UsefulFilterTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly int[] Targets = [252, 0, 2, 252, 0, 4];
    private static readonly int[] Totals = [0, 0, 2, 0, 0, 0];

    [Theory]
    [InlineData("Geodude", false)]
    [InlineData("Ivysaur", false)]
    [InlineData("Caterpie", true)]
    [InlineData("Oddish", true)]
    [InlineData("Pidgey", true)]
    [InlineData("Machop", false)]
    public void Handoff_useful_case(string opponent, bool expected) =>
        Assert.Equal(expected, Evs.IsUseful(Data.Lookup(opponent)!, Totals, Targets));

    [Fact]
    public void Vitamin_is_useful_only_below_100_and_below_target()
    {
        var calcium = Data.Lookup("Calcium")!;

        Assert.True(Evs.IsUseful(calcium, [0, 0, 0, 90, 0, 0], Targets));
        Assert.False(Evs.IsUseful(calcium, [0, 0, 0, 100, 0, 0], Targets));
        Assert.False(Evs.IsUseful(calcium, [0, 0, 0, 0, 0, 0], [0, 0, 0, 0, 0, 0]));
    }
}