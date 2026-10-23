using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 015: integrity of the embedded league.json generated from the FireRed decompilation.</summary>
public class LeagueDataTests
{
    private static readonly League League = League.LoadEmbedded();
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    [Fact]
    public void Trainers_come_in_battle_order()
    {
        Assert.Equal(["lorelei", "bruno", "agatha", "lance", "champion"], League.Trainers.Select(t => t.Id));
    }

    [Fact]
    public void Lorelei_first_battle_and_rematch_match_the_game()
    {
        var first = League.Team("lorelei", LeagueRound.First, Starter.Bulbasaur);
        Assert.Equal(["Dewgong", "Cloyster", "Slowbro", "Jynx", "Lapras"], first.Select(m => m.Species));
        var dewgong = first[0];
        Assert.Equal(52, dewgong.Level);
        Assert.Equal("Thick Fat", dewgong.Ability);
        Assert.Null(dewgong.Item);
        Assert.Equal(["Ice Beam", "Surf", "Hail", "Safeguard"], dewgong.Moves);
        Assert.Equal("Sitrus Berry", first[4].Item);
        Assert.Equal("Water Absorb", first[4].Ability);

        var rematch = League.Team("lorelei", LeagueRound.Rematch, Starter.Bulbasaur);
        Assert.Equal(["Dewgong", "Cloyster", "Piloswine", "Jynx", "Lapras"], rematch.Select(m => m.Species));
        Assert.Equal([64, 63, 63, 66, 66], rematch.Select(m => m.Level));
    }

    [Theory]
    [InlineData(Starter.Bulbasaur, "Charizard", "Gyarados")]
    [InlineData(Starter.Charmander, "Blastoise", "Exeggutor")]
    [InlineData(Starter.Squirtle, "Venusaur", "Arcanine")]
    public void Champion_team_depends_on_the_players_starter(Starter starter, string last, string fifth)
    {
        var team = League.Team("champion", LeagueRound.First, starter);
        Assert.Equal(6, team.Count);
        Assert.Equal(last, team[^1].Species);
        Assert.Equal(fifth, team[4].Species);
        Assert.Equal(last, League.Team("champion", LeagueRound.Rematch, starter)[^1].Species);
    }

    [Fact]
    public void Opponents_list_every_pokemon_of_the_round_in_order()
    {
        var opponents = League.Opponents(LeagueRound.First, Starter.Bulbasaur);
        Assert.Equal(26, opponents.Count);
        Assert.Equal("lorelei", opponents[0].Trainer.Id);
        Assert.Equal("Charizard", opponents[^1].Mon.Species);
    }

    [Fact]
    public void Trainer_pokemon_use_the_first_ability_of_their_species()
    {
        var all = Enum.GetValues<Starter>()
            .SelectMany(s => Enum.GetValues<LeagueRound>().SelectMany(r => League.Opponents(r, s)))
            .Select(o => o.Mon);
        Assert.All(all, mon => Assert.Equal(League.Species(mon.Species)!.Abilities[0], mon.Ability));
        Assert.Equal("Levitate", League.Team("agatha", LeagueRound.First, Starter.Bulbasaur)[0].Ability);
    }

    [Fact]
    public void Every_move_of_a_team_or_learnset_is_known()
    {
        var all = Enum.GetValues<Starter>()
            .SelectMany(s => Enum.GetValues<LeagueRound>().SelectMany(r => League.Opponents(r, s)));
        Assert.All(all, o => Assert.All(o.Mon.Moves, m => Assert.NotNull(League.Move(m))));
        Assert.All(Dex.Species, s =>
        {
            var species = League.Species(s.Name);
            Assert.NotNull(species);
            Assert.InRange(species.Types.Count, 1, 2);
            Assert.All(species.Types, t => Assert.Contains(t, TypeChart.Types));
            Assert.InRange(species.Abilities.Count, 1, 2);
            Assert.All(species.Learnset, l => Assert.NotNull(League.Move(l.Move)));
        });
    }

    [Fact]
    public void Moves_carry_type_power_accuracy_and_damage_kind()
    {
        var thunderbolt = League.Move("Thunderbolt")!;
        Assert.Equal(("Electric", 95, 100, 15, MoveDamage.Normal), (thunderbolt.Type, thunderbolt.Power, thunderbolt.Accuracy, thunderbolt.Pp, thunderbolt.Damage));
        Assert.Equal(MoveDamage.Fixed, League.Move("Dragon Rage")!.Damage);
        Assert.Equal(MoveDamage.Fixed, League.Move("Seismic Toss")!.Damage);
        Assert.Equal(MoveDamage.Variable, League.Move("Return")!.Damage);
        Assert.Equal(MoveDamage.Status, League.Move("Hail")!.Damage);
    }

    [Fact]
    public void Learnsets_have_level_up_tm_hm_and_tutor_moves()
    {
        var vileplume = League.Species("Vileplume")!;
        Assert.Equal(["Grass", "Poison"], vileplume.Types);
        Assert.Contains(vileplume.Learnset, l => l is { Move: "Sludge Bomb", Source: LearnSource.Machine, Machine: "TM36" });
        Assert.Contains(vileplume.Learnset, l => l is { Move: "Solar Beam", Source: LearnSource.Machine, Machine: "TM22" });
        Assert.Contains(vileplume.Learnset, l => l is { Move: "Petal Dance", Source: LearnSource.Level, Level: 44 });
        Assert.Contains(vileplume.Learnset, l => l is { Move: "Body Slam", Source: LearnSource.Tutor });
        Assert.Contains(League.Species("Venusaur")!.Learnset, l => l is { Move: "Frenzy Plant", Source: LearnSource.Tutor });
        Assert.DoesNotContain(League.Species("Ivysaur")!.Learnset, l => l.Move == "Frenzy Plant");
        Assert.Contains(League.Species("Lapras")!.Learnset, l => l is { Move: "Surf", Machine: "HM03" });
        Assert.True(League.Species("Raichu")!.CanLearn("Thunderbolt"));
        Assert.False(League.Species("Raichu")!.CanLearn("Flamethrower"));
    }
}