using EvTrainer.Web.Domain;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 015: Generation III type chart and the physical/special split by type.</summary>
public class TypeChartTests
{
    [Theory]
    [InlineData("Electric", new[] { "Water", "Flying" }, 4.0)]
    [InlineData("Ground", new[] { "Flying" }, 0.0)]
    [InlineData("Ghost", new[] { "Normal" }, 0.0)]
    [InlineData("Normal", new[] { "Ghost" }, 0.0)]
    [InlineData("Fire", new[] { "Water", "Rock" }, 0.25)]
    [InlineData("Ice", new[] { "Dragon", "Flying" }, 4.0)]
    [InlineData("Steel", new[] { "Ice" }, 2.0)]
    [InlineData("Ghost", new[] { "Steel" }, 0.5)]
    [InlineData("Dark", new[] { "Steel" }, 0.5)]
    [InlineData("Psychic", new[] { "Dark" }, 0.0)]
    [InlineData("Poison", new[] { "Steel" }, 0.0)]
    [InlineData("Electric", new[] { "Ground", "Flying" }, 0.0)]
    [InlineData("Water", new[] { "Grass", "Poison" }, 0.5)]
    [InlineData("Bug", new[] { "Psychic", "Dark" }, 4.0)]
    [InlineData("Fighting", new[] { "Normal" }, 2.0)]
    [InlineData("Dragon", new[] { "Dragon" }, 2.0)]
    [InlineData("Rock", new[] { "Fire", "Flying" }, 4.0)]
    [InlineData("Grass", new[] { "Bug", "Steel" }, 0.25)]
    public void Multiplier_follows_the_generation_iii_chart(string attack, string[] defender, double expected) =>
        Assert.Equal(expected, TypeChart.Multiplier(attack, defender));

    [Theory]
    [InlineData("Normal", true)]
    [InlineData("Fighting", true)]
    [InlineData("Ghost", true)]
    [InlineData("Steel", true)]
    [InlineData("Poison", true)]
    [InlineData("Dark", false)]
    [InlineData("Water", false)]
    [InlineData("Dragon", false)]
    [InlineData("Psychic", false)]
    public void Category_depends_only_on_the_type(string type, bool physical) =>
        Assert.Equal(physical, TypeChart.IsPhysical(type));

    [Fact]
    public void Knows_the_seventeen_types() => Assert.Equal(17, TypeChart.Types.Count);
}