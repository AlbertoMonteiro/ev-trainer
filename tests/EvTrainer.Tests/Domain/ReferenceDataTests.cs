using EvTrainer.Web.Domain;
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
        Assert.False(Data.WildPlaces(GameVersion.FireRed).ContainsKey("Milotic"));
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
    public void Keeps_curated_trainer_locations_first_then_every_other_place_of_the_game()
    {
        Assert.Equal("Laboratório do Prof. Oak", Data.TrainersByLocation[0].Name);
        Assert.Equal(["Bulbasaur", "Charmander", "Squirtle"], Data.TrainersByLocation[0].Opponents);
        Assert.Equal(Data.TrainersByLocation.Count, Data.TrainersByLocation.Select(t => t.Name).Distinct().Count());
        Assert.Contains(Data.TrainersByLocation, t => t.Name == "Rota 20" && t.Opponents.Contains("Seaking"));
        Assert.Contains(Data.TrainersByLocation, t => t.Name == "Sevault Canyon");
        Assert.DoesNotContain(Data.TrainersByLocation, t => t.Name == "Rota 19 (parcial)");
    }

    [Fact]
    public void Curated_and_generated_trainers_of_the_same_place_are_merged()
    {
        var route3 = Data.TrainersByLocation.Single(t => t.Name == "Rota 3");

        Assert.Equal("Pidgey", route3.Opponents[0]);
        Assert.Equal(route3.Opponents.Count, route3.Opponents.Distinct().Count());
    }

    [Fact]
    public void Wild_places_come_from_the_game_for_each_version()
    {
        foreach (var version in new[] { GameVersion.FireRed, GameVersion.LeafGreen })
        {
            Assert.Contains("Rota 20", Data.WildPlaces(version)["Tentacool"]);
            Assert.Contains("Monean Chamber", Data.WildPlaces(version)["Unown"]);
        }

        Assert.Contains("Safari Zone", Data.WildPlaces(GameVersion.FireRed)["Scyther"]);
        Assert.False(Data.WildPlaces(GameVersion.LeafGreen).ContainsKey("Scyther"));
        Assert.Contains("Safari Zone", Data.WildPlaces(GameVersion.LeafGreen)["Pinsir"]);
        Assert.False(Data.WildPlaces(GameVersion.FireRed).ContainsKey("Pinsir"));
    }

    [Theory]
    [InlineData(null, GameVersion.FireRed)]
    [InlineData("fireRed", GameVersion.FireRed)]
    [InlineData("leafGreen", GameVersion.LeafGreen)]
    [InlineData("ruby", GameVersion.FireRed)]
    public void Game_version_keys(string? key, GameVersion expected)
    {
        Assert.Equal(expected, GameVersions.Parse(key));
        Assert.Equal(expected, GameVersions.Parse(GameVersions.Key(expected)));
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