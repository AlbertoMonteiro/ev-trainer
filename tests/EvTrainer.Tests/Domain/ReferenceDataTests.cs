using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class ReferenceDataTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    [Fact]
    public void Loads_151_pokemon_and_6_vitamins()
    {
        Assert.Equal(157, Data.Opponents.Count);
        Assert.Equal(151, Data.Opponents.Count(o => o.Dex is >= 1 and <= 151 && !o.IsVitamin));
        var vitamins = Data.Opponents.Where(o => o.IsVitamin).ToList();
        Assert.All(vitamins, v => Assert.Null(v.Dex));
        Assert.Equal(["HP Up", "Protein", "Iron", "Calcium", "Zinc", "Carbos"], vitamins.Select(v => v.Name));
        Assert.All(Data.Opponents, o => Assert.Equal(6, o.EvYield.Length));
    }

    [Fact]
    public void Loads_25_natures_with_5_neutral()
    {
        Assert.Equal(25, Data.Natures.Count);
        Assert.Equal(
            ["Hardy", "Docile", "Serious", "Bashful", "Quirky"],
            Data.Natures.Where(n => n.IsNeutral).Select(n => n.Name));
    }

    [Fact]
    public void Keeps_trainer_locations_in_file_order()
    {
        Assert.Equal(34, Data.TrainersByLocation.Count);
        Assert.Equal("Laboratório do Prof. Oak", Data.TrainersByLocation[0].Name);
        Assert.Equal(["Bulbasaur", "Charmander", "Squirtle"], Data.TrainersByLocation[0].Opponents);
    }

    [Theory]
    [InlineData("Bulbasaur (rival)", "Bulbasaur")]
    [InlineData("Nidoran F", "Nidoran♀")]
    [InlineData("Nidoran M", "Nidoran♂")]
    [InlineData("Desconhecido", "Desconhecido")]
    [InlineData("Caterpie", "Caterpie")]
    public void Canon_applies_aliases(string name, string expected) => Assert.Equal(expected, Data.Canon(name));

    [Fact]
    public void Lookup_resolves_aliases()
    {
        Assert.Equal("Nidoran♀", Data.Lookup("Nidoran F")!.Name);
        Assert.Equal(25, Data.Lookup("Pikachu")!.Dex);
        Assert.Null(Data.Lookup("Agumon"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Xyz")]
    public void FindNature_returns_null_for_empty_or_unknown(string name) => Assert.Null(Data.FindNature(name));

    [Fact]
    public void Exposes_stat_names()
    {
        Assert.Equal(["HP", "Ataque", "Defesa", "SpAtk", "SpDef", "Speed"], Data.StatNames);
        Assert.Equal(["HP", "ATK", "DEF", "SPA", "SPD", "SPE"], Data.StatsShort);
    }
}