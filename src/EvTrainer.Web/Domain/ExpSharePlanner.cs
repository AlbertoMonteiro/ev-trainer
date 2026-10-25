using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.Domain;

/// <summary>A Pokémon of the plan: current EV totals, EV targets and Pokérus (spec 016).</summary>
public sealed record PlanMon(int[] Totals, int[] Targets, bool Pokerus);

/// <summary>Who leads with the Macho Brace and who holds the Exp. Share (A is the first Pokémon of the plan).</summary>
public enum Setup
{
    ALeadsBShares,
    BLeadsAShares,
    AAlone,
    BAlone,
}

/// <summary>Battles in one setup, with the EVs each Pokémon gets per battle.</summary>
public sealed record PlanPhase(Setup Setup, int Battles, int[] GainA, int[] GainB);

/// <summary>
/// The plan: the phases with battles (in <see cref="Setup"/> order), the expected EVs, what is still missing in the
/// stats the opponent gives, the total of battles and the battles of training each one alone with the Macho Brace.
/// </summary>
public sealed record SharePlan(
    IReadOnlyList<PlanPhase> Phases,
    int[] FinalA,
    int[] FinalB,
    int[] LeftA,
    int[] LeftB,
    int Battles,
    int AloneBattles)
{
    /// <summary>The opponent cannot help: any battle would go past a target (or nobody needs its EVs).</summary>
    public bool Useless => Battles == 0;
}

/// <summary>
/// How many battles to fight against one opponent in each Exp. Share setup (spec 016, research R2): never past a
/// target, least left over, then fewest battles, then fewest setups.
/// </summary>
public static class ExpSharePlanner
{
    private sealed record Capacity(int[] PerStat, int Total);

    public static SharePlan Plan(PlanMon a, PlanMon? b, int[] yieldPerBattle)
    {
        var stats = Enumerable.Range(0, StatVector.Count).Where(i => yieldPerBattle[i] > 0).ToArray();
        var capA = CapacityOf(a);
        var capB = b is null ? new Capacity(new int[StatVector.Count], 0) : CapacityOf(b);

        int[] Gain(bool pokerus, bool machoBrace) => [.. yieldPerBattle.Select(y => y * Evs.Multiplier(pokerus, machoBrace))];
        var none = new int[StatVector.Count];
        var gains = new Dictionary<Setup, (int[] A, int[] B)>
        {
            [Setup.ALeadsBShares] = (Gain(a.Pokerus, true), b is null ? none : Gain(b.Pokerus, false)),
            [Setup.BLeadsAShares] = (Gain(a.Pokerus, false), b is null ? none : Gain(b.Pokerus, true)),
            [Setup.AAlone] = (Gain(a.Pokerus, true), none),
            [Setup.BAlone] = (none, b is null ? none : Gain(b.Pokerus, true)),
        };

        // With no partner only A battles alone.
        var withShare = b is not null;
        var limit1 = withShare ? Math.Min(MaxBattles(gains[Setup.ALeadsBShares].A, capA, stats), MaxBattles(gains[Setup.ALeadsBShares].B, capB, stats)) : 0;
        var limit2 = withShare ? Math.Min(MaxBattles(gains[Setup.BLeadsAShares].A, capA, stats), MaxBattles(gains[Setup.BLeadsAShares].B, capB, stats)) : 0;

        (int Left, int Battles, int Setups, int[] Counts)? best = null;
        for (var x1 = 0; x1 <= limit1; x1++)
        {
            for (var x2 = 0; x2 <= limit2; x2++)
            {
                var restA = Rest(capA, stats, (gains[Setup.ALeadsBShares].A, x1), (gains[Setup.BLeadsAShares].A, x2));
                var restB = Rest(capB, stats, (gains[Setup.ALeadsBShares].B, x1), (gains[Setup.BLeadsAShares].B, x2));
                if (restA is null || restB is null)
                {
                    continue;
                }

                var x3 = MaxBattles(gains[Setup.AAlone].A, restA, stats);
                var x4 = withShare ? MaxBattles(gains[Setup.BAlone].B, restB, stats) : 0;
                var left = stats.Sum(i => restA.PerStat[i] - (x3 * gains[Setup.AAlone].A[i]) + restB.PerStat[i] - (x4 * gains[Setup.BAlone].B[i]));
                int[] counts = [x1, x2, x3, x4];
                var candidate = (left, counts.Sum(), counts.Count(c => c > 0), counts);
                if (best is null || IsBetter(candidate, best.Value))
                {
                    best = candidate;
                }
            }
        }

        var chosen = best!.Value.Counts;
        var phases = Enum.GetValues<Setup>()
            .Where(s => chosen[(int)s] > 0)
            .Select(s => new PlanPhase(s, chosen[(int)s], gains[s].A, gains[s].B))
            .ToList();
        var finalA = Final(a.Totals, phases, p => p.GainA);
        var finalB = b is null ? new int[StatVector.Count] : Final(b.Totals, phases, p => p.GainB);
        var aloneA = MaxBattles(gains[Setup.AAlone].A, capA, stats);
        var aloneB = withShare ? MaxBattles(gains[Setup.BAlone].B, capB, stats) : 0;

        return new SharePlan(
            phases,
            finalA,
            finalB,
            Left(capA, a.Totals, finalA, stats),
            b is null ? new int[StatVector.Count] : Left(capB, b.Totals, finalB, stats),
            chosen.Sum(),
            aloneA + aloneB);
    }

    /// <summary>What each stat can still take (target capped at 255) and what the 510 total still allows.</summary>
    private static Capacity CapacityOf(PlanMon mon) => new(
        [.. Enumerable.Range(0, StatVector.Count).Select(i => Math.Max(0, Math.Min(mon.Targets[i], StatVector.MaxEvPerStat) - mon.Totals[i]))],
        Math.Max(0, StatVector.MaxEvTotal - mon.Totals.Sum()));

    /// <summary>Most battles that fit the capacity in every stat the opponent gives and in the total.</summary>
    private static int MaxBattles(int[] gain, Capacity capacity, int[] stats)
    {
        var perBattle = stats.Sum(i => gain[i]);
        if (perBattle == 0)
        {
            return int.MaxValue / 4;
        }

        return Math.Min(capacity.Total / perBattle, stats.Where(i => gain[i] > 0).Min(i => capacity.PerStat[i] / gain[i]));
    }

    /// <summary>Capacity left after the battles, or null when they do not fit.</summary>
    private static Capacity? Rest(Capacity capacity, int[] stats, params (int[] Gain, int Battles)[] used)
    {
        var perStat = (int[])capacity.PerStat.Clone();
        var total = capacity.Total;
        foreach (var (gain, battles) in used)
        {
            foreach (var i in stats)
            {
                perStat[i] -= gain[i] * battles;
                total -= gain[i] * battles;
            }
        }

        return stats.Any(i => perStat[i] < 0) || total < 0 ? null : new Capacity(perStat, total);
    }

    /// <summary>
    /// Least left over, then fewest battles, then fewest setups; on a full tie, more battles in the earlier setups
    /// (A, the selected Pokémon, leading first).
    /// </summary>
    private static bool IsBetter((int Left, int Battles, int Setups, int[] Counts) candidate, (int Left, int Battles, int Setups, int[] Counts) best)
    {
        int[] a = [candidate.Left, candidate.Battles, candidate.Setups, .. candidate.Counts.Select(c => -c)];
        int[] b = [best.Left, best.Battles, best.Setups, .. best.Counts.Select(c => -c)];
        var differs = a.Zip(b).FirstOrDefault(p => p.First != p.Second);
        return differs.First < differs.Second;
    }

    private static int[] Final(int[] totals, IReadOnlyList<PlanPhase> phases, Func<PlanPhase, int[]> gain) =>
        [.. Enumerable.Range(0, StatVector.Count).Select(i => totals[i] + phases.Sum(p => p.Battles * gain(p)[i]))];

    private static int[] Left(Capacity capacity, int[] totals, int[] final, int[] stats) =>
        [.. Enumerable.Range(0, StatVector.Count).Select(i => stats.Contains(i) ? capacity.PerStat[i] - (final[i] - totals[i]) : 0)];
}