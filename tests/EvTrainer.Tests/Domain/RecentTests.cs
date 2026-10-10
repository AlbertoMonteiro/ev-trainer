using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class RecentTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    [Fact]
    public void Returns_up_to_six_distinct_canonical_names_newest_first()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(
            ("Weedle", 1, 1), ("Bulbasaur (rival)", 1, 1), ("Caterpie", 2, 1), ("Pidgey", 1, 1), ("Abra", 1, 1),
            ("Agumon", 1, 1), ("Oddish", 1, 1), ("Bulbasaur", 1, 2), ("Caterpie", 1, 1), ("Geodude", 1, 1));

        var recent = Evs.Recent(pokemon.Events, Data);

        Assert.Equal(["Geodude", "Caterpie", "Bulbasaur", "Oddish", "Abra", "Pidgey"], recent.Select(o => o.Name));
    }

    [Fact]
    public void Empty_log_has_no_recents() => Assert.Empty(Evs.Recent(Pokemons.Squirtle().Events, Data));

    [Fact]
    public void Species_beyond_kanto_appear_in_recents()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Milotic", 1, 1), ("Caterpie", 1, 1));

        Assert.Equal(["Caterpie", "Milotic"], Evs.Recent(pokemon.Events, Data).Select(o => o.Name));
    }
}