using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class ReferenceDataTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    [Fact]
    public void Loads_386_species_and_6_vitamins()
    {
        Assert.Equal(392, Data.Opponents.Count);
        Assert.Equal(Enumerable.Range(1, 386), Data.Opponents.Take(386).Select(o => o.Dex ?? 0));
        Assert.All(Data.Opponents.Take(386), o => Assert.False(o.IsVitamin));
        var vitamins = Data.Opponents.Skip(386).ToList();
        Assert.All(vitamins, v => Assert.True(v.IsVitamin && v.Dex is null));
        Assert.Equal(["HP Up", "Protein", "Iron", "Calcium", "Zinc", "Carbos"], vitamins.Select(v => v.Name));
        Assert.All(Data.Opponents, o => Assert.Equal(6, o.EvYield.Length));
        Assert.All(Data.Opponents.Take(386), o => Assert.True(o.EvYield.Sum() >= 1, $"{o.Name} has no EV yield"));
    }

    [Fact]
    public void Kanto_yields_match_between_the_curated_file_and_the_generated_pokedex()
    {
        var dex = Pokedex.LoadEmbedded();

        var differences = Data.Opponents
            .Where(o => o.Dex is >= 1 and <= 151)
            .Where(o => !dex.Find(o.Name)!.EvYield.SequenceEqual(o.EvYield))
            .Select(o => o.Name)
            .ToList();

        Assert.Empty(differences);
    }

    [Fact]
    public void Species_beyond_kanto_are_opponents_without_locations()
    {
        var milotic = Data.Lookup("Milotic")!;

        Assert.Equal(350, milotic.Dex);
        Assert.Equal([0, 0, 0, 0, 2, 0], milotic.EvYield);
        Assert.Equal("", milotic.WildLocation);
        Assert.Equal("Rota 3, 4, 22", Data.Lookup("Mankey")!.WildLocation);
        Assert.Equal(34, Data.TrainersByLocation.Count);
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