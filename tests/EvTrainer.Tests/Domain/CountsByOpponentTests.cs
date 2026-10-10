using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class CountsByOpponentTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    [Fact]
    public void Matches_count_for_every_opponent_in_one_pass()
    {
        var events = Pokemons.Squirtle().WithEvents(
            ("Nidoran F", 2, 1), ("Nidoran♀", 1, 2), ("Bulbasaur (rival)", 1, 1), ("Bulbasaur", 3, 1),
            ("Milotic", 4, 1), ("Agumon", 9, 1), ("Caterpie", 1, 4)).Events;

        var counts = Evs.CountsByOpponent(events, Data);

        Assert.All(Data.Opponents, o =>
            Assert.Equal(Evs.CountFor(events, o.Name, Data), counts.GetValueOrDefault(o.Name)));
        Assert.Equal(3, counts["Nidoran♀"]);
        Assert.Equal(4, counts["Bulbasaur"]);
        Assert.Equal(4, counts["Milotic"]);
        Assert.False(counts.ContainsKey("Agumon"));
    }
}