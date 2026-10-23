using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>How well the team hits an opponent with its best damaging move.</summary>
public enum CoverageLevel
{
    /// <summary>No damaging move, or every move has no effect.</summary>
    None,
    Resisted,
    Neutral,
    SuperEffective,
}

/// <param name="Member">Index of the team member that uses <paramref name="Move"/>.</param>
public sealed record MoveHit(int Member, string Move, double Multiplier);

public sealed record OpponentCoverage(LeagueOpponent Opponent, MoveHit? Best, CoverageLevel Level);

/// <param name="Unanswered">Opponents the team only hits resisted, or not at all.</param>
public sealed record CoverageSummary(int SuperEffective, int Neutral, int Unanswered, int Total);

/// <summary>A damaging move of <paramref name="Opponent"/> that hits <paramref name="Member"/> super effectively.</summary>
public sealed record Threat(LeagueOpponent Opponent, int Member, string Move, double Multiplier);

/// <summary>
/// Plans a team against the Pokémon League by type (spec 015): Gen III type chart, the abilities that cancel or reduce
/// a move (research R5), coverage, threats and a greedy move suggestion (research R6). No damage calculation.
/// </summary>
public sealed class LeaguePlanner(League league, Pokedex dex)
{
    /// <summary>Nominal power of variable-power moves for the suggestion; the others are never suggested.</summary>
    private static readonly Dictionary<string, int> VariablePower = new(StringComparer.Ordinal)
    {
        ["Return"] = 102,
        ["Frustration"] = 102,
        ["Hidden Power"] = 70,
        ["Magnitude"] = 71,
        ["Low Kick"] = 60,
    };

    /// <summary>Moves the suggestion avoids: they faint the user, need sleep or a setup, or only work once.</summary>
    private static readonly HashSet<string> NeverSuggested =
        ["Explosion", "Self-Destruct", "Dream Eater", "Snore", "Focus Punch", "Fake Out", "Spit Up", "Memento"];

    /// <summary>Moves that cost an extra turn (charge or recharge): half their power for the suggestion.</summary>
    private static readonly HashSet<string> TwoTurns =
        ["Hyper Beam", "Blast Burn", "Frenzy Plant", "Hydro Cannon", "Solar Beam", "Sky Attack", "Razor Wind", "Skull Bash", "Future Sight"];

    /// <summary>Moves that hurt the user (recoil): three quarters of their power for the suggestion.</summary>
    private static readonly HashSet<string> Recoil = ["Double-Edge", "Take Down", "Submission", "Volt Tackle"];

    public League League => league;

    /// <summary>
    /// Multiplier of <paramref name="move"/> against a defender (0, ⅛ … 4), or null for status moves. Fixed-damage moves
    /// hit 1× unless the type or the ability makes the defender immune.
    /// </summary>
    public double? Effectiveness(LeagueMove move, IReadOnlyList<string> defenderTypes, string defenderAbility)
    {
        if (!move.DealsDamage)
        {
            return null;
        }

        var types = TypeChart.Multiplier(move.Type, defenderTypes);
        var multiplier = move.Damage == MoveDamage.Fixed ? (types == 0 ? 0 : 1) : types;
        return (defenderAbility, move.Type) switch
        {
            ("Levitate", "Ground") or ("Water Absorb", "Water") or ("Volt Absorb", "Electric") or ("Flash Fire", "Fire") => 0,
            ("Wonder Guard", _) when multiplier < 2 => 0,
            ("Thick Fat", "Fire" or "Ice") when move.Damage != MoveDamage.Fixed => multiplier / 2,
            _ => multiplier,
        };
    }

    /// <summary>For every Pokémon of the plan's round, the team's best damaging move against it.</summary>
    public IReadOnlyList<OpponentCoverage> Coverage(LeaguePlan plan)
    {
        var moves = TeamMoves(plan);
        return
        [
            .. league.Opponents(plan.Round, plan.Starter).Select(opponent =>
            {
                var target = league.Species(opponent.Mon.Species)!;
                MoveHit? best = null;
                var bestStrength = -1.0;
                foreach (var (member, move, strength) in moves)
                {
                    var multiplier = Effectiveness(move, target.Types, opponent.Mon.Ability)!.Value;
                    if (best is null || multiplier > best.Multiplier || (multiplier == best.Multiplier && strength > bestStrength))
                    {
                        best = new MoveHit(member, move.Name, multiplier);
                        bestStrength = strength;
                    }
                }

                return new OpponentCoverage(opponent, best, LevelOf(best?.Multiplier ?? 0));
            }),
        ];
    }

    public CoverageSummary Summary(IReadOnlyList<OpponentCoverage> coverage) => new(
        coverage.Count(c => c.Level == CoverageLevel.SuperEffective),
        coverage.Count(c => c.Level == CoverageLevel.Neutral),
        coverage.Count(c => c.Level is CoverageLevel.Resisted or CoverageLevel.None),
        coverage.Count);

    /// <summary>Every (opponent, member) pair where a damaging move of the opponent is super effective.</summary>
    public IReadOnlyList<Threat> Threats(LeaguePlan plan)
    {
        List<Threat> threats = [];
        foreach (var opponent in league.Opponents(plan.Round, plan.Starter))
        {
            for (var member = 0; member < plan.Members.Count; member++)
            {
                var defender = plan.Members[member];
                var types = league.Species(defender.Species)!.Types;
                foreach (var name in opponent.Mon.Moves)
                {
                    if (league.Move(name) is { DealsDamage: true } move
                        && Effectiveness(move, types, defender.Ability) is >= 2 and var multiplier)
                    {
                        threats.Add(new Threat(opponent, member, name, multiplier));
                    }
                }
            }
        }

        return threats;
    }

    /// <summary>Opponents with damaging moves that all hit <paramref name="member"/> resisted or not at all.</summary>
    public IReadOnlyList<LeagueOpponent> Walls(LeaguePlan plan, int member)
    {
        var defender = plan.Members[member];
        var types = league.Species(defender.Species)!.Types;
        return
        [
            .. league.Opponents(plan.Round, plan.Starter).Where(opponent =>
            {
                var multipliers = opponent.Mon.Moves
                    .Select(league.Move)
                    .OfType<LeagueMove>()
                    .Where(m => m.DealsDamage)
                    .Select(m => Effectiveness(m, types, defender.Ability)!.Value)
                    .ToList();
                return multipliers.Count > 0 && multipliers.All(m => m < 1);
            }),
        ];
    }

    /// <summary>
    /// Fills the free (unlocked, empty) move slots of every member, or only of <paramref name="member"/>, greedily
    /// (research R6): first more opponents hit super effectively, then more hit at least neutrally, then a stronger
    /// best hit against each opponent; ties go to a type the member does not have yet and then to the stronger move.
    /// Moves the member already has that are not locked are replaced.
    /// </summary>
    public LeaguePlan Suggest(LeaguePlan plan, int? member = null)
    {
        var opponents = league.Opponents(plan.Round, plan.Starter)
            .Select(o => (Types: league.Species(o.Mon.Species)!.Types, o.Mon.Ability))
            .ToList();
        List<List<string>> moves = [];
        for (var i = 0; i < plan.Members.Count; i++)
        {
            var current = plan.Members[i];
            moves.Add(member is null || member == i ? [.. current.Locked] : [.. current.Moves]);
        }

        // Best value (multiplier × strength) and multiplier of the team against each opponent.
        var bestValue = new double[opponents.Count];
        var bestMultiplier = new double[opponents.Count];
        for (var i = 0; i < moves.Count; i++)
        {
            foreach (var name in moves[i])
            {
                Apply(i, league.Move(name)!);
            }
        }

        var candidates = Enumerable.Range(0, plan.Members.Count)
            .Where(i => member is null || member == i)
            .SelectMany(i => league.Species(plan.Members[i].Species)!.Learnset
                .Select(l => l.Move)
                .Distinct()
                .Select(league.Move)
                .OfType<LeagueMove>()
                .Select(m => (Member: i, Move: m, Strength: Strength(plan.Members[i].Species, m)))
                .Where(c => c.Strength > 0 && !NeverSuggested.Contains(c.Move.Name)))
            .OrderBy(c => c.Member)
            .ThenBy(c => c.Move.Name, StringComparer.Ordinal)
            .ToList();

        while (true)
        {
            (int Member, LeagueMove Move, double Strength)? pick = null;
            (int, int, double, int, double) pickScore = default;
            foreach (var candidate in candidates)
            {
                var slots = moves[candidate.Member];
                if (slots.Count >= PlanMember.MaxMoves || slots.Contains(candidate.Move.Name))
                {
                    continue;
                }

                var (superEffective, neutral, total) = (0, 0, 0.0);
                for (var o = 0; o < opponents.Count; o++)
                {
                    var multiplier = Effectiveness(candidate.Move, opponents[o].Types, opponents[o].Ability)!.Value;
                    var best = Math.Max(bestMultiplier[o], multiplier);
                    superEffective += best >= 2 ? 1 : 0;
                    neutral += best >= 1 ? 1 : 0;
                    total += Math.Max(bestValue[o], multiplier * candidate.Strength);
                }

                var newType = slots.Any(s => league.Move(s)!.Type == candidate.Move.Type) ? 0 : 1;
                var score = (superEffective, neutral, Math.Round(total, 6), newType, candidate.Strength);
                if (pick is null || score.CompareTo(pickScore) > 0)
                {
                    pick = candidate;
                    pickScore = score;
                }
            }

            if (pick is not { } chosen)
            {
                break;
            }

            moves[chosen.Member].Add(chosen.Move.Name);
            Apply(chosen.Member, chosen.Move);
        }

        return plan with
        {
            Members = [.. plan.Members.Select((m, i) => member is null || member == i ? m with { Moves = moves[i] } : m)],
        };

        void Apply(int index, LeagueMove move)
        {
            if (!move.DealsDamage)
            {
                return;
            }

            var strength = Strength(plan.Members[index].Species, move);
            for (var o = 0; o < opponents.Count; o++)
            {
                var multiplier = Effectiveness(move, opponents[o].Types, opponents[o].Ability)!.Value;
                bestMultiplier[o] = Math.Max(bestMultiplier[o], multiplier);
                bestValue[o] = Math.Max(bestValue[o], multiplier * strength);
            }
        }
    }

    /// <summary>
    /// How hard <paramref name="move"/> hits when <paramref name="species"/> uses it, ignoring the target: power ×
    /// accuracy × 1.5 for the same type (½ for moves with a charge or recharge turn, ¾ with recoil) × the attack stat it uses relative to the species' best attack stat.
    /// </summary>
    public double Strength(string species, LeagueMove move)
    {
        var power = move.Damage switch
        {
            MoveDamage.Normal => move.Power,
            MoveDamage.Variable => VariablePower.GetValueOrDefault(move.Name),
            _ => 0,
        };
        if (power == 0)
        {
            return 0;
        }

        var accuracy = move.Accuracy == 0 ? 1.0 : move.Accuracy / 100.0;
        var stab = league.Species(species)!.Types.Contains(move.Type) ? 1.5 : 1.0;
        var turns = TwoTurns.Contains(move.Name) ? 0.5 : Recoil.Contains(move.Name) ? 0.75 : 1.0;
        var ratio = 1.0;
        if (dex.Find(species) is { } entry && Math.Max(entry.Base[1], entry.Base[3]) is > 0 and var top)
        {
            ratio = (TypeChart.IsPhysical(move.Type) ? entry.Base[1] : entry.Base[3]) / (double)top;
        }

        return power * accuracy * stab * turns * ratio;
    }

    private static CoverageLevel LevelOf(double multiplier) => multiplier switch
    {
        >= 2 => CoverageLevel.SuperEffective,
        >= 1 => CoverageLevel.Neutral,
        > 0 => CoverageLevel.Resisted,
        _ => CoverageLevel.None,
    };

    private List<(int Member, LeagueMove Move, double Strength)> TeamMoves(LeaguePlan plan) =>
    [
        .. plan.Members.SelectMany((m, i) => m.Moves
            .Select(league.Move)
            .OfType<LeagueMove>()
            .Where(move => move.DealsDamage)
            .Select(move => (i, move, Strength(m.Species, move)))),
    ];
}