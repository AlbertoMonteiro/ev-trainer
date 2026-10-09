using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>Gen 3 EV rules (HANDOFF §2.1, §4.1–4.4, §4.6).</summary>
public static class Evs
{
    /// <summary>
    /// Recomputes EV totals from scratch, in event order: 255 per stat, 510 overall; vitamins add 10
    /// only while the stat is below 100, cap at 100 and ignore the multiplier. Unknown events add nothing.
    /// </summary>
    public static int[] Totals(IReadOnlyList<BattleEvent> events, ReferenceData data)
    {
        var ev = new int[StatVector.Count];
        var sum = 0;
        foreach (var e in events)
        {
            if (data.Lookup(e.Opponent) is not { } opponent)
            {
                continue;
            }

            for (var k = 0; k < Math.Max(1, e.Count); k++)
            {
                for (var i = 0; i < StatVector.Count; i++)
                {
                    if (opponent.EvYield[i] == 0)
                    {
                        continue;
                    }

                    int add;
                    if (opponent.IsVitamin)
                    {
                        if (ev[i] >= StatVector.VitaminCap)
                        {
                            continue;
                        }

                        add = Math.Min(10, StatVector.VitaminCap - ev[i]);
                    }
                    else
                    {
                        add = opponent.EvYield[i] * Math.Max(1, e.Multiplier);
                    }

                    add = Math.Max(0, Math.Min(add, Math.Min(StatVector.MaxEvPerStat - ev[i], StatVector.MaxEvTotal - sum)));
                    ev[i] += add;
                    sum += add;
                }
            }
        }

        return ev;
    }

    /// <summary>1, 2 (Pokérus or Macho Brace) or 4 (both).</summary>
    public static int Multiplier(bool pokerus, bool machoBrace) => (pokerus ? 2 : 1) * (machoBrace ? 2 : 1);

    /// <summary>
    /// Useful when every stat the opponent gives is still below its target and, for vitamins, below 100.
    /// </summary>
    public static bool IsUseful(Opponent opponent, int[] totals, int[] targets) =>
        opponent.EvYield.Select((yield, i) => yield == 0
            || (totals[i] < targets[i] && (!opponent.IsVitamin || totals[i] < StatVector.VitaminCap)))
        .All(ok => ok);

    /// <summary>Last distinct known opponents (canonical names), newest first.</summary>
    public static IReadOnlyList<Opponent> Recent(IReadOnlyList<BattleEvent> events, ReferenceData data, int max = 6)
    {
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        List<Opponent> recent = [with(capacity: max)];
        for (var i = events.Count - 1; i >= 0 && recent.Count < max; i--)
        {
            if (data.Lookup(events[i].Opponent) is { } opponent && seen.Add(opponent.Name))
            {
                recent.Add(opponent);
            }
        }

        return recent;
    }

    /// <summary>How many times an opponent was defeated (sum of counts by canonical name).</summary>
    public static int CountFor(IReadOnlyList<BattleEvent> events, string opponent, ReferenceData data)
    {
        var canonical = data.Canon(opponent);
        return events.Where(e => data.Canon(e.Opponent) == canonical).Sum(e => Math.Max(1, e.Count));
    }
}