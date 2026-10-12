using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class SearchTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly OpponentSearch Search = new(Data, LocationNames.LoadEmbedded());

    private static List<string> Names(string query) => [.. Search.Find(query).Select(h => h.Opponent.Name)];

    [Fact]
    public void Rota_4_includes_wild_and_trainer_matches()
    {
        var hits = Search.Find("rota 4");

        Assert.Contains(hits, h => h.Opponent.Name == "Mankey" && h.TrainerLocations.Count == 0);
        Assert.Contains(hits, h => h.Opponent.Name == "Paras" && h.TrainerLocations.Contains("Rota 4"));
        Assert.Contains(hits, h => h.Opponent.Name == "Parasect" && h.TrainerLocations.Contains("Rota 4"));
    }

    [Theory]
    [InlineData("route 22", "rota 22")]
    [InlineData("route 3", "rota 3")]
    [InlineData("route 13", "rota 13")]
    [InlineData("pewter gym", "ginásio de pewter")]
    [InlineData("prof. oak's lab", "laboratório do prof. oak")]
    [InlineData("fishing", "pesca")]
    public void English_place_names_find_the_same_opponents_as_the_portuguese_ones(string english, string portuguese)
    {
        Assert.NotEmpty(Names(portuguese));
        Assert.Equal(Names(portuguese), Names(english));
    }

    [Fact]
    public void English_trainer_matches_keep_the_portuguese_place_as_identity()
    {
        var hit = Assert.Single(Search.Find("pewter gym"), h => h.Opponent.Name == "Onix");

        Assert.Equal(["Ginásio de Pewter (Brock)"], hit.TrainerLocations);
    }

    [Fact]
    public void Route_1_does_not_match_routes_10_to_19() =>
        Assert.Equal(["Pidgey", "Rattata"], Names("route 1"));

    [Fact]
    public void Rota_1_does_not_match_routes_10_to_19() =>
        Assert.Equal(["Pidgey", "Rattata"], Names("rota 1"));

    [Fact]
    public void Gym_leader_name_finds_the_gym_team() =>
        Assert.Equal(["Geodude", "Onix", "Sandshrew"], Names("brock").Order());

    [Fact]
    public void Nidoran_f_finds_the_female()
    {
        var names = Names("nidoran f");

        Assert.Contains("Nidoran♀", names);
        Assert.DoesNotContain("Nidoran♂", names);
    }

    [Theory]
    [InlineData("#25")]
    [InlineData("pikachu")]
    [InlineData("PIKACHU")]
    public void Finds_by_dex_number_or_name(string query) => Assert.Contains("Pikachu", Names(query));

    [Fact]
    public void Accents_are_ignored() => Assert.Contains("Bulbasaur", Names("laboratorio"));

    [Fact]
    public void Keeps_reference_order()
    {
        var names = Names("rota 22");

        Assert.Equal(names.OrderBy(n => Data.Opponents.ToList().FindIndex(o => o.Name == n)), names);
    }

    [Fact]
    public void Blank_query_returns_nothing() => Assert.Empty(Names("   "));

    [Theory]
    [InlineData("milo")]
    [InlineData("350")]
    [InlineData("#350")]
    public void Finds_species_beyond_kanto(string query) => Assert.Contains("Milotic", Names(query));

    [Fact]
    public void Species_beyond_kanto_have_no_trainer_locations()
    {
        var hit = Assert.Single(Search.Find("zigza"));

        Assert.Equal("Zigzagoon", hit.Opponent.Name);
        Assert.Empty(hit.TrainerLocations);
    }
}