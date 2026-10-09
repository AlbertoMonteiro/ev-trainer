using EvTrainer.Web.Domain;

namespace EvTrainer.Tests.Domain;

public class StatsTests
{
    [Theory]
    // HP = floor((2B + IV + floor(EV/4)) * L / 100) + L + 10
    [InlineData(44, 31, 0, 5, 10, true, 20)]
    [InlineData(44, 12, 0, 5, 10, true, 20)]
    [InlineData(44, 11, 0, 5, 10, true, 19)]
    [InlineData(59, 24, 38, 21, 10, true, 62)]
    // Other = floor((floor((2B + IV + floor(EV/4)) * L / 100) + 5) * natMul / 10)
    [InlineData(43, 31, 0, 5, 11, false, 11)]
    [InlineData(43, 14, 0, 5, 11, false, 11)]
    [InlineData(43, 13, 0, 5, 11, false, 9)]
    [InlineData(48, 4, 0, 5, 9, false, 9)]
    [InlineData(48, 3, 0, 5, 9, false, 8)]
    [InlineData(65, 26, 27, 21, 10, false, 39)]
    public void Uses_the_gen3_integer_formula(int @base, int iv, int ev, int level, int natureMultiplier, bool isHp, int expected) =>
        Assert.Equal(expected, StatFormula.Value(@base, iv, ev, level, natureMultiplier, isHp));

    [Fact]
    public void Nature_multiplier_is_integer_math_not_floating_point()
    {
        // inner + 5 = 50 → 50 * 11 / 10 = 55 exactly; with 1.1 in floating point 50 * 1.1 = 55.000000000000007.
        Assert.Equal(55, StatFormula.Value(@base: 20, iv: 5, ev: 0, level: 100, natureMultiplier: 11, isHp: false));
    }
}