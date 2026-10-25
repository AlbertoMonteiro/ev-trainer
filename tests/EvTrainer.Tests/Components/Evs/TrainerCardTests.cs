using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Components.Evs;

/// <summary>Spec 016, US6: trainers found by name or class in the opponent search (contracts/trainer-search.md).</summary>
public class TrainerCardTests : AppTestContext
{
    private IRenderedComponent<Home> SearchFor(string query)
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
        var cut = Render<Home>();
        cut.Find("input.search").Input(query);
        return cut;
    }

    private int SpAtk(string id) => EvTrainer.Web.Domain.Evs.Totals(Store.All[id].Events, Data)[3];

    [Fact]
    public void Bernie_shows_both_teams_with_the_evs_per_battle()
    {
        var cut = SearchFor("bernie");

        var card = cut.Find(".trainer-results .trainer-card[data-trainer='ENGINEER_BERNIE']");
        Assert.Contains("Engineer Bernie", card.TextContent);
        var stages = card.QuerySelectorAll(".trainer-stage");
        Assert.Equal(["0", "3"], stages.Select(s => s.GetAttribute("data-stage")));
        Assert.Contains(T("TrainerCard.Many", T("Guide.Pokemon", "Magneton", 28), 3), stages[1].TextContent);
        Assert.Contains(T("TrainerCard.PerBattle", T("Evs.Gain.Part", 6, T("Stat.Long.3"))), stages[1].TextContent);
        Assert.Contains(T("TrainerCard.PerBattle", T("Evs.Gain.Part", 4, T("Stat.Long.3"))), stages[0].TextContent);
        Assert.NotEmpty(cut.FindAll(".opponent-grid button.opponent[data-opp='Magneton']"));
    }

    [Fact]
    public void Recording_the_whole_battle_counts_every_pokemon_and_undoes_at_once()
    {
        var cut = SearchFor("bernie");
        Store.SetExpShare("jolt");

        cut.Find(".trainer-card .trainer-stage[data-stage='3'] button.record-team").Click();

        Assert.Equal(6, SpAtk("buzz"));
        Assert.Equal(6, SpAtk("jolt"));
        Assert.Contains("Engineer Bernie", Toasts.Message);
        Assert.Contains("Jolteon", Toasts.Message);

        cut.Find(".toast button").Click();

        Assert.Equal(0, SpAtk("buzz"));
        Assert.Equal(0, SpAtk("jolt"));
    }

    [Fact]
    public void Planning_opens_the_plan_with_the_trainer_and_team()
    {
        var cut = SearchFor("bernie");

        cut.Find(".trainer-card .trainer-stage[data-stage='3'] button.plan-team").Click();

        Assert.NotNull(cut.Find(".exp-plan-view"));
        Assert.Equal("3", cut.Find("select.plan-stage option[selected]").GetAttribute("value"));
        Assert.Equal(["8", "4"], cut.FindAll(".plan-phase strong.battles").Select(b => b.TextContent.Split(' ')[0]));
    }

    [Fact]
    public void A_double_battle_trainer_has_no_plan()
    {
        var trainer = Guide.Trainers.First(t => t.Double);
        var cut = SearchFor($"{trainer.TrainerClass} {trainer.Name}");

        var card = cut.Find($".trainer-card[data-trainer='{trainer.Id}']");
        Assert.Empty(card.QuerySelectorAll("button.plan-team"));
        Assert.NotEmpty(card.QuerySelectorAll("button.record-team"));
        Assert.Contains(T("TrainerCard.DoubleWhy"), card.TextContent);
    }

    [Fact]
    public void Requires_the_elite_four_mark()
    {
        var trainer = Guide.Trainers.First(t => !t.RequiresEliteFour && t.Stages.Any(s => s.RequiresEliteFour));
        var cut = SearchFor($"{trainer.TrainerClass} {trainer.Name}");

        Assert.NotEmpty(cut.FindAll($".trainer-card[data-trainer='{trainer.Id}'] .trainer-stage.requires-e4"));
    }

    [Fact]
    public void No_search_no_cards()
    {
        var cut = SearchFor("");

        Assert.Empty(cut.FindAll(".trainer-results"));
    }
}