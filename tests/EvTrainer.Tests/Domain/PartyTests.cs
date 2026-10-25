using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Domain;

public class PartyTests
{
    private static readonly DateTimeOffset Base = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Pokémon "p1".."pN" with Order = i; stamps[i] (hours after Base) or null.</summary>
    private static Dictionary<string, TrainingPokemon> Make(int count, Func<int, bool?> party, Func<int, int?>? hours = null)
    {
        var all = new Dictionary<string, TrainingPokemon>(StringComparer.Ordinal);
        for (var i = 1; i <= count; i++)
        {
            var pokemon = Pokemons.Pidgey();
            pokemon.Order = i;
            pokemon.Party = party(i);
            pokemon.UpdatedAt = hours?.Invoke(i) is { } h ? Base.AddHours(h) : null;
            all[$"p{i}"] = pokemon;
        }

        return all;
    }

    private static string[] InParty(Dictionary<string, TrainingPokemon> all) =>
        [.. all.Where(kv => kv.Value.Party == true).Select(kv => kv.Key).Order(StringComparer.Ordinal)];

    [Fact]
    public void Old_data_with_up_to_six_all_join_the_party()
    {
        var all = Make(3, _ => null);

        Party.Normalize(all);

        Assert.All(all.Values, p => Assert.True(p.Party));
    }

    [Fact]
    public void Old_data_with_more_than_six_keeps_the_six_most_recently_changed()
    {
        var all = Make(8, _ => null, i => i);

        Party.Normalize(all);

        Assert.Equal(["p3", "p4", "p5", "p6", "p7", "p8"], InParty(all));
        Assert.False(all["p1"].Party);
        Assert.False(all["p2"].Party);
    }

    [Fact]
    public void Old_data_without_stamps_keeps_the_lowest_order()
    {
        var all = Make(8, _ => null);

        Party.Normalize(all);

        Assert.Equal(["p1", "p2", "p3", "p4", "p5", "p6"], InParty(all));
    }

    [Fact]
    public void Seven_in_the_party_drops_the_oldest()
    {
        var all = Make(7, _ => true, i => i == 4 ? 0 : 10 + i);

        Party.Normalize(all);

        Assert.False(all["p4"].Party);
        Assert.Equal(6, InParty(all).Length);
    }

    [Fact]
    public void Unknown_ones_fill_the_free_places_by_recency()
    {
        var all = Make(7, i => i <= 4 ? true : null, i => i switch { 5 => 1, 6 => 3, 7 => 2, _ => 0 });

        Party.Normalize(all);

        Assert.Equal(["p1", "p2", "p3", "p4", "p6", "p7"], InParty(all));
        Assert.False(all["p5"].Party);
    }

    [Fact]
    public void A_stored_holder_loses_the_exp_share()
    {
        var all = Make(7, _ => true, i => i);
        all["p1"].ExpShare = true;

        var changed = Party.Normalize(all);

        Assert.False(all["p1"].Party);
        Assert.False(all["p1"].ExpShare);
        Assert.Equal(["p1"], changed);
    }

    [Fact]
    public void Returns_only_the_changed_ids_and_never_stamps()
    {
        var all = Make(3, i => i == 2 ? null : true, i => i);
        var before = all.ToDictionary(kv => kv.Key, kv => kv.Value.UpdatedAt);

        var changed = Party.Normalize(all);

        Assert.Equal(["p2"], changed);
        Assert.All(all, kv => Assert.Equal(before[kv.Key], kv.Value.UpdatedAt));
        Assert.Empty(Party.Normalize(all));
    }

    [Fact]
    public void Members_follow_the_order_then_the_id()
    {
        var all = Make(4, i => i != 2);
        all["p4"].Order = 0;
        all["p3"].Order = 1;

        Assert.Equal(["p4", "p1", "p3"], Party.Members(all));
    }
}