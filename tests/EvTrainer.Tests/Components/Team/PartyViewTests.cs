using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components.Team;

/// <summary>Spec 016, US2: the page that edits the current party (contracts/party.md).</summary>
public class PartyViewTests : AppTestContext
{
    /// <summary>"p1".."pN" (Order = i) with the given party marks, then the party page opened from the menu.</summary>
    private IRenderedComponent<Home> Open(int count, Func<int, bool> party)
    {
        for (var i = 1; i <= count; i++)
        {
            var pokemon = Pokemons.Pidgey();
            pokemon.Order = i;
            pokemon.Party = party(i);
            Repository.Stored[$"p{i}"] = pokemon;
        }

        Store.Load();
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel button.party").Click();
        return cut;
    }

    private static List<string?> Ids(IRenderedComponent<Home> cut, string rows) =>
        [.. cut.FindAll(rows).Select(r => r.GetAttribute("data-id"))];

    private static List<string?> MenuIds(IRenderedComponent<Home> cut)
    {
        cut.Find("header button.menu").Click();
        List<string?> ids = [.. cut.FindAll(".menu-panel .pokemon-list:not(.opened) button.pokemon-item").Select(i => i.GetAttribute("data-id"))];
        cut.Find(".menu-backdrop").Click();
        return ids;
    }

    [Fact]
    public void Shows_the_party_and_the_stored_pokemon()
    {
        var cut = Open(8, i => i <= 6);

        Assert.Contains(T("Party.Members", 6, 6), cut.Find(".party-members h3").TextContent);
        Assert.Contains(T("Party.Stored", 2), cut.Find(".party-stored h3").TextContent);
        Assert.Equal(["p1", "p2", "p3", "p4", "p5", "p6"], Ids(cut, ".party-row"));
        Assert.Equal(["p7", "p8"], Ids(cut, ".stored-row"));
        Assert.Equal(T("Party.MoveUp", "Pidgey"), cut.Find(".party-row[data-id='p2'] button.move-up").GetAttribute("aria-label"));
        Assert.Equal(T("Party.MoveDown", "Pidgey"), cut.Find(".party-row[data-id='p2'] button.move-down").GetAttribute("aria-label"));
    }

    [Fact]
    public void Store_and_join_change_the_menu()
    {
        var cut = Open(7, i => i <= 6);

        cut.Find(".party-row[data-id='p3'] button.leave").Click();
        Assert.Equal(["p1", "p2", "p4", "p5", "p6"], MenuIds(cut));

        cut.Find(".stored-row[data-id='p7'] button.join").Click();
        Assert.Equal(["p1", "p2", "p4", "p5", "p6", "p7"], MenuIds(cut));
    }

    [Fact]
    public void With_a_full_party_a_stored_one_swaps_with_the_chosen_member()
    {
        var cut = Open(7, i => i <= 6);

        Assert.Empty(cut.FindAll(".stored-row button.join"));
        cut.Find(".stored-row[data-id='p7'] button.swap").Click();
        var choices = cut.FindAll(".swap-choices button.swap-with");
        Assert.Equal(6, choices.Count);

        cut.Find(".swap-choices button.swap-with[data-id='p2']").Click();

        Assert.Equal(["p1", "p3", "p4", "p5", "p6", "p7"], Ids(cut, ".party-row"));
        Assert.Equal(["p2"], Ids(cut, ".stored-row"));
    }

    [Fact]
    public void Moving_up_and_down_reorders_the_party()
    {
        var cut = Open(3, _ => true);

        cut.Find(".party-row[data-id='p3'] button.move-up").Click();
        cut.Find(".party-row[data-id='p1'] button.move-down").Click();

        Assert.Equal(["p3", "p1", "p2"], Ids(cut, ".party-row"));
        Assert.Equal(["p3", "p1", "p2"], MenuIds(cut));
    }

    [Fact]
    public void Opening_selects_and_returns_to_the_tabs()
    {
        var cut = Open(7, i => i <= 6);

        cut.Find(".stored-row[data-id='p7'] button.open").Click();

        Assert.Equal("p7", Store.SelectedId);
        Assert.Empty(cut.FindAll(".party-view"));
        Assert.NotNull(cut.Find("[role=tablist]"));
    }

    [Fact]
    public void An_empty_party_says_so()
    {
        var cut = Open(2, _ => false);

        Assert.Contains(T("Party.Empty"), cut.Find(".party-members").TextContent);
        Assert.Equal(["p1", "p2"], Ids(cut, ".stored-row"));
    }

    [Fact]
    public void Back_closes_the_page()
    {
        var cut = Open(2, _ => true);

        cut.Find("button.close-party").Click();

        Assert.Empty(cut.FindAll(".party-view"));
    }
}