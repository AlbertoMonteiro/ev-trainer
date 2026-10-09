using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class PokedexTests
{
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    private static List<string> Line(string name) => [.. Dex.LineFor(Dex.Find(name)!).Select(s => s.Name)];

    [Fact]
    public void Has_the_386_species_of_generation_III()
    {
        Assert.Equal(386, Dex.Species.Count);
        Assert.Equal(Enumerable.Range(1, 386), Dex.Species.Select(s => s.Dex));
        Assert.All(Dex.Species, s => Assert.Equal(6, s.Base.Length));
    }

    [Theory]
    [InlineData("Squirtle", new[] { 44, 48, 65, 50, 64, 43 })]
    [InlineData("Butterfree", new[] { 60, 45, 50, 80, 80, 70 })]
    [InlineData("Pikachu", new[] { 35, 55, 30, 50, 40, 90 })]
    [InlineData("Pidgeot", new[] { 83, 80, 75, 70, 70, 91 })]
    [InlineData("Nidoran♀", new[] { 55, 47, 52, 40, 40, 41 })]
    public void Uses_generation_III_base_stats(string name, int[] expected) =>
        Assert.Equal(expected, Dex.Find(name)!.Base);

    [Theory]
    [InlineData("squ", "Squirtle")]
    [InlineData("SQUIRTLE", "Squirtle")]
    [InlineData("#25", "Pikachu")]
    [InlineData("25", "Pikachu")]
    [InlineData("nidoran f", "Nidoran♀")]
    [InlineData("ho-oh", "Ho-Oh")]
    public void Search_by_name_or_number(string query, string expected) =>
        Assert.Equal(expected, Dex.Search(query)[0].Name);

    [Fact]
    public void Blank_search_returns_nothing() => Assert.Empty(Dex.Search(" "));

    [Fact]
    public void Line_goes_from_the_base_form_to_the_last_unique_evolution()
    {
        Assert.Equal(["Squirtle", "Wartortle", "Blastoise"], Line("Squirtle"));
        Assert.Equal(["Squirtle", "Wartortle", "Blastoise"], Line("Wartortle"));
        Assert.Equal(["Pichu", "Pikachu", "Raichu"], Line("Pikachu"));
        Assert.Equal(["Mr. Mime"], Line("Mr. Mime"));
        Assert.Equal(["Zubat", "Golbat", "Crobat"], Line("Zubat"));
    }

    [Fact]
    public void Branching_evolutions_stop_and_offer_the_options()
    {
        Assert.Equal(["Eevee"], Line("Eevee"));
        Assert.Equal(
            ["Vaporeon", "Jolteon", "Flareon", "Espeon", "Umbreon"],
            Dex.EvolutionsOf(Dex.Find("Eevee")!).Select(s => s.Name));
        Assert.Equal(["Oddish", "Gloom"], Line("Oddish"));
        Assert.Equal(["Oddish", "Gloom", "Bellossom"], Line("Bellossom"));
        Assert.Empty(Dex.EvolutionsOf(Dex.Find("Blastoise")!));
    }
}