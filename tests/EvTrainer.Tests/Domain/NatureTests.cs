using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class NatureTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    [Fact]
    public void Timid_raises_speed_and_lowers_attack()
    {
        var timid = Data.FindNature("Timid")!;
        Assert.Equal([10, 9, 10, 10, 10, 11], Enumerable.Range(0, 6).Select(timid.Multiplier));
        Assert.Equal(5, timid.Increases);
        Assert.Equal(1, timid.Decreases);
    }

    [Fact]
    public void Hp_is_never_affected()
    {
        Assert.All(Data.Natures, n => Assert.Equal(10, n.Multiplier(0)));
    }

    [Theory]
    [InlineData("Hardy")]
    [InlineData("Docile")]
    [InlineData("Serious")]
    [InlineData("Bashful")]
    [InlineData("Quirky")]
    public void Neutral_natures_are_10_everywhere(string name)
    {
        var nature = Data.FindNature(name)!;
        Assert.True(nature.IsNeutral);
        Assert.All(Enumerable.Range(0, 6), i => Assert.Equal(10, nature.Multiplier(i)));
    }

    [Fact]
    public void Every_non_neutral_nature_has_one_11_and_one_9()
    {
        foreach (var nature in Data.Natures.Where(n => !n.IsNeutral))
        {
            var multipliers = Enumerable.Range(0, 6).Select(nature.Multiplier).ToList();
            Assert.Equal(1, multipliers.Count(m => m == 11));
            Assert.Equal(1, multipliers.Count(m => m == 9));
        }
    }
}