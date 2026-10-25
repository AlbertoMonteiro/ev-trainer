using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class SearchTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly OpponentSearch Search = new(Data, LocationNames.LoadEmbedded(), VsSeekerGuide.LoadEmbedded());

    private static List<string> Names(string query) => [.. Search.Find(query, GameVersion.FireRed).Select(h => h.Opponent.Name)];

    [Fact]
    public void Rota_4_includes_wild_and_trainer_matches()
    {
        var hits = Search.Find("rota 4", GameVersion.FireRed);

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
    public void English_place_names_find_the_same_opponents_as_the_portuguese_ones(string english, string portuguese)
    {
        Assert.NotEmpty(Names(portuguese));
        Assert.Equal(Names(portuguese), Names(english));
    }

    [Fact]
    public void Every_kanto_route_has_trainers_or_wild_pokemon_from_the_game_data()
    {
        for (var route = 1; route <= 25; route++)
        {
            Assert.NotEmpty(Names($"rota {route}"));
        }
    }

    [Fact]
    public void Route_20_lists_its_trainers_and_its_wild_pokemon()
    {
        var hits = Search.Find("rota 20", GameVersion.FireRed);

        Assert.Contains(hits, h => h.Opponent.Name == "Seaking" && h.TrainerLocations.Contains("Rota 20"));
        Assert.Contains(hits, h => h.Opponent.Name == "Tentacool");
        Assert.Equal(Names("rota 20"), Names("route 20"));
    }

    [Theory]
    [InlineData("sevault canyon", "Chansey")]
    [InlineData("kindle road", "Machop")]
    [InlineData("ginásio de cinnabar", "Arcanine")]
    [InlineData("blaine", "Arcanine")]
    [InlineData("pokémon league", "Dragonite")]
    [InlineData("berry forest", "Drowzee")]
    [InlineData("monean chamber", "Unown")]
    public void Islands_gyms_and_dungeons_are_searchable(string query, string expected) =>
        Assert.Contains(expected, Names(query));

    [Theory]
    [InlineData(GameVersion.FireRed, "Scyther", "Pinsir")]
    [InlineData(GameVersion.LeafGreen, "Pinsir", "Scyther")]
    public void Safari_zone_lists_the_wild_pokemon_of_the_chosen_version(GameVersion version, string present, string absent)
    {
        var names = Search.Find("safari zone", version).Select(h => h.Opponent.Name).ToList();

        Assert.Contains(present, names);
        Assert.DoesNotContain(absent, names);
    }

    [Fact]
    public void Route_24_in_leaf_green_has_wild_bellsprout_and_oddish_only_from_trainers()
    {
        var hits = Search.Find("rota 24", GameVersion.LeafGreen);

        Assert.Contains(hits, h => h.Opponent.Name == "Bellsprout");
        Assert.All(hits.Where(h => h.Opponent.Name == "Oddish"), h => Assert.NotEmpty(h.TrainerLocations));
        Assert.Contains(Search.Find("rota 24", GameVersion.FireRed), h => h.Opponent.Name == "Oddish");
    }

    [Theory]
    [InlineData(GameVersion.FireRed)]
    [InlineData(GameVersion.LeafGreen)]
    public void Wild_matches_by_place_come_only_from_the_chosen_version(GameVersion version)
    {
        var places = Data.WildPlaces(version);
        foreach (var place in places.Values.SelectMany(p => p).Distinct())
        {
            var wildHits = Search.Find(place, version)
                .Where(h => h.TrainerLocations.Count == 0 && !h.Opponent.IsVitamin)
                .Where(h => !TextFold.Fold(h.Opponent.Name).Contains(TextFold.Fold(place), StringComparison.Ordinal));
            foreach (var hit in wildHits)
            {
                Assert.True(
                    places.TryGetValue(hit.Opponent.Name, out var found) && found.Any(p => TextFold.Fold(p).Contains(TextFold.Fold(place), StringComparison.Ordinal)),
                    $"{hit.Opponent.Name} is not wild in {place} ({version})");
            }
        }
    }

    [Fact]
    public void Trainers_are_the_same_in_both_versions()
    {
        static List<string> Trainers(GameVersion version) =>
            [.. Search.Find("rota 20", version).Where(h => h.TrainerLocations.Count > 0).Select(h => h.Opponent.Name)];

        Assert.NotEmpty(Trainers(GameVersion.FireRed));
        Assert.Equal(Trainers(GameVersion.FireRed), Trainers(GameVersion.LeafGreen));
    }

    [Theory]
    [InlineData(GameVersion.FireRed)]
    [InlineData(GameVersion.LeafGreen)]
    public void Vitamins_are_found_in_celadon_in_both_versions(GameVersion version) =>
        Assert.Contains(Search.Find("celadon", version), h => h.Opponent.IsVitamin);

    [Fact]
    public void English_trainer_matches_keep_the_portuguese_place_as_identity()
    {
        var hit = Assert.Single(Search.Find("pewter gym", GameVersion.FireRed), h => h.Opponent.Name == "Onix");

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
        var hit = Assert.Single(Search.Find("zigza", GameVersion.FireRed));

        Assert.Equal("Zigzagoon", hit.Opponent.Name);
        Assert.Empty(hit.TrainerLocations);
    }

    private static List<string> Trainers(string query) => [.. Search.FindTrainers(query).Select(t => t.Id)];

    [Theory]
    [InlineData("bernie")]
    [InlineData("Engineer Bernie")]
    [InlineData("BÉRNIE")]
    [InlineData("  engineer bernie ")]
    public void Trainers_are_found_by_name_or_class_and_name(string query) =>
        Assert.Equal(["ENGINEER_BERNIE"], Trainers(query));

    [Fact]
    public void A_class_finds_all_its_trainers_in_guide_order() =>
        Assert.Equal(["ENGINEER_BRAXTON", "ENGINEER_BERNIE"], Trainers("engineer"));

    [Theory]
    [InlineData("rota 11")]
    [InlineData("brock")]
    [InlineData("")]
    [InlineData("   ")]
    public void Places_unknown_trainers_and_empty_queries_find_no_trainer(string query) => Assert.Empty(Trainers(query));

    [Fact]
    public void A_trainer_search_lists_the_species_of_its_teams_with_the_place()
    {
        var hits = Search.Find("bernie", GameVersion.FireRed);

        Assert.Contains(hits, h => h.Opponent.Name == "Magnemite" && h.TrainerLocations.Contains("Rota 11"));
        Assert.Contains(hits, h => h.Opponent.Name == "Magneton" && h.TrainerLocations.Contains("Rota 11"));
    }

    [Fact]
    public void A_species_matched_twice_is_listed_once()
    {
        var hits = Search.Find("magneton", GameVersion.FireRed);
        var trainerHits = Search.Find("engineer", GameVersion.FireRed);

        Assert.Single(hits, h => h.Opponent.Name == "Magneton");
        Assert.Single(trainerHits, h => h.Opponent.Name == "Magneton");
    }
}