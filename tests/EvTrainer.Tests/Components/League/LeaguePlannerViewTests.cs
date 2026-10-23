using AngleSharp.Dom;

using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components.League;

/// <summary>Spec 015: the Elite Four planner page.</summary>
public class LeaguePlannerViewTests : AppTestContext
{
    private IRenderedComponent<Home> Open()
    {
        Store.Load();
        var cut = Render<Home>();
        cut.Find("header button.menu").Click();
        cut.Find(".menu-panel button.league").Click();
        return cut;
    }

    private static IElement Mon(IRenderedComponent<Home> cut, string trainer, int index) =>
        cut.Find($".league-trainer[data-trainer='{trainer}'] li.league-mon[data-index='{index}']");

    private void Plan(params PlanMember[] members) =>
        Store.UpdatePlan(Store.Plan with { Members = [.. members] });

    private static PlanMember Member(string species, params string[] moves) =>
        LeaguePlans.NewMember(species, AppTestContext.League) with { Moves = moves };

    [Fact]
    public void Opens_from_the_menu_and_closes()
    {
        var cut = Open();

        Assert.Equal(T("League.Title"), cut.Find(".league-view h2").TextContent.Trim());
        cut.Find(".league-view button.close-league").Click();
        Assert.Empty(cut.FindAll(".league-view"));
    }

    [Fact]
    public void Shows_the_league_in_battle_order_with_every_detail()
    {
        var cut = Open();

        Assert.Equal(["lorelei", "bruno", "agatha", "lance", "champion"], cut.FindAll(".league-trainer").Select(t => t.GetAttribute("data-trainer")));
        Assert.Equal(T("League.Trainer.champion"), cut.Find(".league-trainer[data-trainer='champion'] h3").TextContent.Trim());
        var dewgong = Mon(cut, "lorelei", 0);
        Assert.Contains("Dewgong", dewgong.QuerySelector(".species")!.TextContent);
        Assert.Contains(T("League.Mon.Level", 52), dewgong.TextContent);
        Assert.Equal(["Water", "Ice"], dewgong.QuerySelectorAll(".types .type").Select(t => t.TextContent.Trim()));
        Assert.Contains(T("League.Mon.Ability", "Thick Fat"), dewgong.TextContent);
        Assert.Equal(["Ice Beam", "Surf", "Hail", "Safeguard"], dewgong.QuerySelectorAll(".moves .move .name").Select(m => m.TextContent.Trim()));
        Assert.Contains(T("League.Mon.Item", "Sitrus Berry"), Mon(cut, "lorelei", 4).TextContent);
        Assert.Contains("Charizard", Mon(cut, "champion", 5).TextContent);
    }

    [Fact]
    public void Round_and_starter_change_the_teams_and_are_remembered()
    {
        var cut = Open();

        cut.Find(".league-round button[data-round='rematch']").Click();
        cut.Find(".league-starter button[data-starter='squirtle']").Click();

        Assert.Equal("true", cut.Find(".league-round button[data-round='rematch']").GetAttribute("aria-pressed"));
        Assert.Contains("Piloswine", Mon(cut, "lorelei", 2).TextContent);
        Assert.Contains("Venusaur", Mon(cut, "champion", 5).TextContent);
        Assert.Equal((LeagueRound.Rematch, Starter.Squirtle), (Repository.Plan!.Round, Repository.Plan.Starter));
    }

    [Fact]
    public void Adding_a_member_and_choosing_a_move_updates_the_coverage()
    {
        var cut = Open();
        Assert.Contains(T("League.Summary.Unanswered", 26), cut.Find(".league-summary").TextContent);

        cut.Find(".league-team .species-search").Input("raichu");
        cut.Find(".league-team .species-option[data-name='Raichu']").Click();
        cut.Find(".plan-member[data-index='0'] select.move-slot[data-slot='0']").Change("Thunderbolt");

        Assert.Equal(["Thunderbolt"], Store.Plan.Members[0].Moves);
        var gyarados = Mon(cut, "lance", 0);
        Assert.Contains("level-super", gyarados.QuerySelector(".cover")!.ClassList);
        Assert.Equal(T("League.Cover.Best", "Raichu", "Thunderbolt", "4×"), gyarados.QuerySelector(".cover")!.TextContent.Trim());
        var onix = Mon(cut, "bruno", 0);
        Assert.Contains("level-none", onix.QuerySelector(".cover")!.ClassList);
        Assert.Equal(T("League.Cover.None"), onix.QuerySelector(".cover")!.TextContent.Trim());
        Assert.DoesNotContain(T("League.Summary.Unanswered", 26), cut.Find(".league-summary").TextContent);
    }

    [Fact]
    public void Move_choices_only_offer_what_the_species_learns_with_where_it_comes_from()
    {
        Plan(Member("Raichu"));
        var cut = Open();

        var options = cut.FindAll(".plan-member[data-index='0'] select.move-slot[data-slot='0'] option").ToList();
        var thunderbolt = options.First(o => o.GetAttribute("value") == "Thunderbolt");
        Assert.Contains("TM24", thunderbolt.TextContent);
        Assert.Contains("Electric", thunderbolt.TextContent);
        Assert.DoesNotContain(options, o => o.GetAttribute("value") == "Flamethrower");
        Assert.Contains(options, o => o.GetAttribute("value") == "Thunder Wave");
    }

    [Fact]
    public void Threats_and_walls_show_on_the_opponents_and_on_the_members()
    {
        Plan(Member("Charizard"), Member("Gengar"));
        var cut = Open();

        var lapras = Mon(cut, "lorelei", 4);
        Assert.Contains(T("League.Threat", "Lapras", "Charizard"), lapras.QuerySelector(".threats")!.TextContent);
        Assert.Contains("Surf", lapras.QuerySelector(".threats")!.TextContent);
        Assert.Contains(lapras.QuerySelectorAll(".moves .move"), m => m.ClassList.Contains("threat") && m.TextContent.Contains("Surf"));

        var charizard = cut.Find(".plan-member[data-index='0']");
        Assert.Contains("Rock Tomb", charizard.QuerySelector(".member-threats")!.TextContent);
        var gengar = cut.Find(".plan-member[data-index='1']");
        Assert.Contains("Hitmonlee", gengar.QuerySelector(".member-walls")!.TextContent);
    }

    [Fact]
    public void Suggestion_fills_the_moves_and_keeps_locked_ones()
    {
        Plan(Member("Raichu", "Thunder Wave"));
        var cut = Open();
        cut.Find(".plan-member[data-index='0'] button.lock[data-move='Thunder Wave']").Click();
        Assert.Equal(["Thunder Wave"], Store.Plan.Members[0].Locked);

        cut.Find("button.suggest-team").Click();

        Assert.Equal(4, Store.Plan.Members[0].Moves.Count);
        Assert.Contains("Thunder Wave", Store.Plan.Members[0].Moves);
        Assert.Contains("Thunderbolt", Store.Plan.Members[0].Moves);
    }

    [Fact]
    public void Suggestion_for_one_member_and_removing_members()
    {
        Plan(Member("Raichu"), Member("Golem"));
        var cut = Open();

        cut.Find(".plan-member[data-index='1'] button.suggest-member").Click();
        Assert.Empty(Store.Plan.Members[0].Moves);
        Assert.Equal(4, Store.Plan.Members[1].Moves.Count);

        cut.Find(".plan-member[data-index='0'] button.remove-member").Click();
        Assert.Equal(["Golem"], Store.Plan.Members.Select(m => m.Species));
    }

    [Fact]
    public void Changing_the_species_keeps_only_the_moves_it_learns_and_says_so()
    {
        Plan(Member("Raichu", "Thunderbolt", "Thunder Wave"));
        var cut = Open();

        cut.Find(".plan-member[data-index='0'] button.change-species").Click();
        cut.Find(".plan-member[data-index='0'] .species-search").Input("gengar");
        cut.Find(".plan-member[data-index='0'] .species-option[data-name='Gengar']").Click();

        Assert.Equal("Gengar", Store.Plan.Members[0].Species);
        Assert.Equal(["Thunderbolt"], Store.Plan.Members[0].Moves);
        Assert.Contains(T("League.Member.Dropped", "Gengar"), cut.Find(".plan-member[data-index='0']").TextContent);
    }

    [Fact]
    public void Ability_choice_appears_only_for_species_with_two()
    {
        Plan(Member("Lapras"), Member("Gengar"));
        var cut = Open();

        cut.Find(".plan-member[data-index='0'] select.ability").Change("Shell Armor");

        Assert.Equal("Shell Armor", Store.Plan.Members[0].Ability);
        Assert.Empty(cut.FindAll(".plan-member[data-index='1'] select.ability"));
        Assert.Contains("Levitate", cut.Find(".plan-member[data-index='1'] .ability").TextContent);
    }

    [Fact]
    public void The_team_is_full_at_six()
    {
        Plan([.. Enumerable.Repeat(Member("Pidgey"), 6)]);
        var cut = Open();

        Assert.Empty(cut.FindAll(".league-team .species-search"));
        Assert.Contains(T("League.Team.Full"), cut.Find(".league-team").TextContent);
    }
}