using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components.Training;

/// <summary>Spec 016, US3–US5: the Exp. Share battle plan (contracts/exp-share-plan.md).</summary>
public class ExpSharePlanViewTests : AppTestContext
{
    /// <summary>Electabuzz (needs 120 Sp. Atk, selected) and Jolteon (needs 96, holds the Exp. Share), plan page open.</summary>
    private IRenderedComponent<Home> OpenPlan(Action? before = null)
    {
        var buzz = Pokemons.Pidgey();
        buzz.CurrentSpecies = "Electabuzz";
        buzz.Targets = [0, 0, 0, 120, 0, 0];
        Seed(buzz, "buzz");
        var jolt = Pokemons.Pidgey();
        jolt.CurrentSpecies = "Jolteon";
        jolt.Order = 2;
        jolt.Targets = [0, 0, 0, 96, 0, 0];
        Seed(jolt, "jolt");
        Store.Select("buzz");
        Store.SetExpShare("jolt");
        before?.Invoke();
        var cut = Render<Home>();
        cut.Find(".summary button.plan-battles").Click();
        return cut;
    }

    private static void PickBernieRematch(IRenderedComponent<Home> cut)
    {
        cut.Find("input.plan-search").Input("bernie");
        cut.Find("button.pick-trainer[data-trainer='ENGINEER_BERNIE']").Click();
        cut.Find("select.plan-stage").Change("3");
    }

    private static List<string> Battles(IRenderedComponent<Home> cut) =>
        [.. cut.FindAll(".plan-phase strong.battles").Select(b => b.TextContent.Split(' ')[0])];

    private int SpAtk(string id) => EvTrainer.Web.Domain.Evs.Totals(Store.All[id].Events, Data)[3];

    [Fact]
    public void Starts_with_the_selected_and_the_holder_from_the_party()
    {
        var cut = OpenPlan(() =>
        {
            var stored = Pokemons.Pidgey();
            stored.Order = 3;
            stored.Party = false;
            Seed(stored, "stored");
            Store.Select("buzz");
        });

        Assert.Equal("buzz", cut.Find("select.plan-a option[selected]").GetAttribute("value"));
        Assert.Equal("jolt", cut.Find("select.plan-b option[selected]").GetAttribute("value"));
        Assert.DoesNotContain("stored", cut.FindAll("select.plan-a option, select.plan-b option").Select(o => o.GetAttribute("value")));
    }

    [Fact]
    public void Bernie_with_three_magneton_takes_eight_plus_four_battles()
    {
        var cut = OpenPlan();

        PickBernieRematch(cut);

        Assert.Equal(["8", "4"], Battles(cut));
        Assert.Equal(["ALeadsBShares", "BLeadsAShares"], cut.FindAll(".plan-phase").Select(p => p.GetAttribute("data-setup")));
        Assert.Contains(T("Plan.Lead", "Electabuzz"), cut.Find(".plan-phase").TextContent);
        Assert.Contains(T("Plan.Holder", "Jolteon"), cut.Find(".plan-phase").TextContent);
        Assert.Equal(T("Plan.Total", 12, 18, 6), cut.Find(".plan-total").TextContent.Trim());
        Assert.Empty(cut.FindAll(".plan-left"));
    }

    [Fact]
    public void What_is_left_is_shown()
    {
        var cut = OpenPlan(() =>
        {
            Store.All["buzz"].Targets = [0, 0, 0, 100, 0, 0];
            Store.All["jolt"].Targets = [0, 0, 0, 90, 0, 0];
        });

        PickBernieRematch(cut);

        Assert.Equal(T("Plan.Left", T("Evs.Gain.Part", 4, T("Stat.Long.3")), "Electabuzz"), cut.Find(".plan-left").TextContent.Trim());
    }

    [Fact]
    public void A_wild_opponent_works()
    {
        var cut = OpenPlan();

        cut.Find("input.plan-search").Input("magnemite");
        cut.Find("button.pick-wild[data-name='Magnemite']").Click();

        Assert.NotEmpty(cut.FindAll(".plan-phase"));
        Assert.Empty(cut.FindAll("button.record-battle"));
    }

    [Fact]
    public void A_useless_opponent_says_so()
    {
        var cut = OpenPlan();

        cut.Find("input.plan-search").Input("caterpie");
        cut.Find("button.pick-wild[data-name='Caterpie']").Click();

        Assert.Equal(T("Plan.Useless"), cut.Find(".plan-useless").TextContent.Trim());
    }

    [Fact]
    public void Double_battle_trainers_cannot_be_picked()
    {
        var trainer = Guide.Trainers.First(t => t.Double);
        var cut = OpenPlan();

        cut.Find("input.plan-search").Input($"{trainer.TrainerClass} {trainer.Name}");

        Assert.True(cut.Find($"button.pick-trainer[data-trainer='{trainer.Id}']").HasAttribute("disabled"));
    }

    [Fact]
    public void Using_a_setup_configures_the_game_and_can_be_undone()
    {
        var cut = OpenPlan();
        PickBernieRematch(cut);

        cut.FindAll(".plan-phase button.use-setup")[1].Click();

        Assert.Equal("jolt", Store.SelectedId);
        Assert.True(Store.All["jolt"].MachoBrace);
        Assert.False(Store.All["buzz"].MachoBrace);
        Assert.Equal("buzz", Store.ExpShareHolderId);
        Assert.Equal(T("Plan.Applied", "Jolteon", "Electabuzz"), Toasts.Message);

        cut.Find(".toast button").Click();

        Assert.Equal("buzz", Store.SelectedId);
        Assert.Equal("jolt", Store.ExpShareHolderId);
    }

    [Fact]
    public void Recording_a_battle_updates_the_plan()
    {
        var cut = OpenPlan();
        PickBernieRematch(cut);
        cut.FindAll(".plan-phase button.use-setup")[0].Click();

        cut.Find("button.record-battle").Click();

        Assert.Equal(12, SpAtk("buzz"));
        Assert.Equal(6, SpAtk("jolt"));
        Assert.Equal(["7", "4"], Battles(cut));

        cut.Find(".toast button").Click();
        Assert.Equal(["8", "4"], Battles(cut));
    }

    [Fact]
    public void A_stored_lead_is_asked_to_join_the_party()
    {
        var cut = OpenPlan(() =>
        {
            Store.LeaveParty("buzz");
            Store.Select("buzz");
        });
        PickBernieRematch(cut);

        cut.FindAll(".plan-phase button.use-setup")[1].Click();

        Assert.Contains(T("Plan.Join", "Electabuzz"), cut.Find(".plan-join").TextContent);
        cut.Find(".plan-join button.confirm-join").Click();

        Assert.True(Store.IsInParty("buzz"));
        Assert.Equal("buzz", Store.ExpShareHolderId);
    }

    [Fact]
    public void A_full_party_offers_the_party_page()
    {
        var cut = OpenPlan(() =>
        {
            for (var i = 3; i <= 7; i++)
            {
                var other = Pokemons.Pidgey();
                other.Order = i;
                other.Party = i <= 6;
                Seed(other, $"o{i}");
            }

            Store.LeaveParty("buzz");
            Store.JoinParty("o7");
            Store.Select("buzz");
        });
        PickBernieRematch(cut);

        cut.FindAll(".plan-phase button.use-setup")[1].Click();
        cut.Find(".plan-join button.open-party").Click();

        Assert.NotNull(cut.Find(".party-view"));
    }
}