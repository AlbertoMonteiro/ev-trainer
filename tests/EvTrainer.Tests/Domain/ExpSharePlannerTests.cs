using EvTrainer.Web.Domain;

namespace EvTrainer.Tests.Domain;

/// <summary>Spec 016, research R2 and contracts/exp-share-plan.md.</summary>
public class ExpSharePlannerTests
{
    private static int[] SpAtk(int value) => [0, 0, 0, value, 0, 0];

    private static PlanMon Mon(int target, bool pokerus = false) => new(new int[6], SpAtk(target), pokerus);

    private static int[] Battles(SharePlan plan) =>
        [.. Enum.GetValues<Setup>().Select(s => plan.Phases.FirstOrDefault(p => p.Setup == s)?.Battles ?? 0)];

    [Theory]
    // # | meta A | meta B | rend. | Pokérus A | Pokérus B | fases | total | sobra | sozinhos
    [InlineData(120, 96, 6, false, false, new[] { 8, 4, 0, 0 }, 12, 0, 18)]
    [InlineData(252, 252, 6, false, false, new[] { 14, 14, 0, 0 }, 28, 0, 42)]
    [InlineData(120, 96, 6, true, false, new[] { 0, 8, 1, 0 }, 9, 0, 13)]
    [InlineData(120, 0, 6, false, false, new[] { 0, 0, 10, 0 }, 10, 0, 10)]
    [InlineData(100, 90, 6, false, false, new[] { 7, 2, 0, 2 }, 11, 4, 15)]
    [InlineData(40, 40, 4, false, false, new[] { 4, 2, 0, 1 }, 7, 0, 10)]
    public void Reference_cases(int targetA, int targetB, int yield, bool pokerusA, bool pokerusB, int[] phases, int total, int left, int alone)
    {
        var plan = ExpSharePlanner.Plan(Mon(targetA, pokerusA), Mon(targetB, pokerusB), SpAtk(yield));

        Assert.Equal(phases, Battles(plan));
        Assert.Equal(total, plan.Battles);
        Assert.Equal(left, plan.LeftA.Sum() + plan.LeftB.Sum());
        Assert.Equal(alone, plan.AloneBattles);
    }

    [Fact]
    public void The_example_of_the_request_ends_exactly_at_the_targets()
    {
        var plan = ExpSharePlanner.Plan(Mon(120), Mon(96), SpAtk(6));

        Assert.Equal(SpAtk(120), plan.FinalA);
        Assert.Equal(SpAtk(96), plan.FinalB);
        var first = plan.Phases[0];
        Assert.Equal(Setup.ALeadsBShares, first.Setup);
        Assert.Equal(SpAtk(12), first.GainA);
        Assert.Equal(SpAtk(6), first.GainB);
        var second = plan.Phases[1];
        Assert.Equal(Setup.BLeadsAShares, second.Setup);
        Assert.Equal(SpAtk(6), second.GainA);
        Assert.Equal(SpAtk(12), second.GainB);
        Assert.False(plan.Useless);
    }

    [Fact]
    public void Without_a_partner_only_a_trains_alone()
    {
        var plan = ExpSharePlanner.Plan(Mon(120), null, SpAtk(6));

        var phase = Assert.Single(plan.Phases);
        Assert.Equal((Setup.AAlone, 10), (phase.Setup, phase.Battles));
        Assert.Equal(10, plan.AloneBattles);
        Assert.Equal(new int[6], plan.FinalB);
    }

    [Fact]
    public void An_opponent_nobody_needs_is_useless()
    {
        var plan = ExpSharePlanner.Plan(Mon(0), Mon(0), SpAtk(6));

        Assert.True(plan.Useless);
        Assert.Empty(plan.Phases);
        Assert.Equal(0, plan.Battles);
    }

    [Fact]
    public void A_pokemon_that_would_overflow_another_stat_never_takes_part()
    {
        // 2 Attack and 1 Speed per battle; A needs Attack but its Speed is complete.
        int[] yield = [0, 2, 0, 0, 0, 1];
        var a = new PlanMon([0, 0, 0, 0, 0, 100], [0, 100, 0, 0, 0, 100], false);
        var b = new PlanMon(new int[6], [0, 40, 0, 0, 0, 20], false);

        var plan = ExpSharePlanner.Plan(a, b, yield);

        var phase = Assert.Single(plan.Phases);
        Assert.Equal((Setup.BAlone, 10), (phase.Setup, phase.Battles));
        Assert.Equal(new[] { 0, 0, 0, 0, 0, 100 }, plan.FinalA);
    }

    [Fact]
    public void The_510_total_limits_the_plan()
    {
        var a = new PlanMon([255, 245, 0, 0, 0, 0], [255, 255, 0, 100, 0, 0], false);

        var plan = ExpSharePlanner.Plan(a, null, SpAtk(1));

        Assert.Equal(5, plan.Battles);
        Assert.Equal(510, plan.FinalA.Sum());
    }

    [Fact]
    public void Targets_above_255_count_up_to_255()
    {
        var a = new PlanMon(SpAtk(250), SpAtk(300), false);

        var plan = ExpSharePlanner.Plan(a, null, SpAtk(1));

        Assert.Equal(2, plan.Battles);
        Assert.Equal(SpAtk(254), plan.FinalA);
        Assert.Equal(SpAtk(1), plan.LeftA);
    }

    [Fact]
    public void Matches_a_full_search_of_the_four_setups()
    {
        var random = new Random(16);
        for (var round = 0; round < 200; round++)
        {
            int[] yield = [0, 0, 0, 0, 0, 0];
            yield[random.Next(6)] = random.Next(1, 4);
            if (random.Next(3) == 0)
            {
                yield[random.Next(6)] += random.Next(1, 3);
            }

            var a = new PlanMon(new int[6], [.. Enumerable.Range(0, 6).Select(_ => random.Next(0, 21))], random.Next(2) == 0);
            var b = new PlanMon(new int[6], [.. Enumerable.Range(0, 6).Select(_ => random.Next(0, 21))], random.Next(2) == 0);

            var plan = ExpSharePlanner.Plan(a, b, yield);
            var (left, battles) = FullSearch(a, b, yield);

            Assert.Equal(left, plan.LeftA.Sum() + plan.LeftB.Sum());
            Assert.Equal(battles, plan.Battles);
            for (var i = 0; i < 6; i++)
            {
                Assert.True(plan.FinalA[i] <= Math.Max(a.Targets[i], a.Totals[i]) || yield[i] == 0);
                Assert.True(plan.FinalB[i] <= Math.Max(b.Targets[i], b.Totals[i]) || yield[i] == 0);
            }
        }
    }

    /// <summary>Every combination of the four setups (from zero EVs): least left over, then fewest battles.</summary>
    private static (int Left, int Battles) FullSearch(PlanMon a, PlanMon b, int[] yield)
    {
        int ma = a.Pokerus ? 2 : 1, mb = b.Pokerus ? 2 : 1;
        int[][] gainA = [[.. yield.Select(y => 2 * ma * y)], [.. yield.Select(y => ma * y)], [.. yield.Select(y => 2 * ma * y)], new int[6]];
        int[][] gainB = [[.. yield.Select(y => mb * y)], [.. yield.Select(y => 2 * mb * y)], new int[6], [.. yield.Select(y => 2 * mb * y)]];
        var best = (Left: int.MaxValue, Battles: int.MaxValue);
        for (var x1 = 0; x1 <= 20; x1++)
        {
            for (var x2 = 0; x2 <= 20; x2++)
            {
                for (var x3 = 0; x3 <= 20; x3++)
                {
                    for (var x4 = 0; x4 <= 20; x4++)
                    {
                        int[] n = [x1, x2, x3, x4];
                        var left = 0;
                        var fits = true;
                        for (var i = 0; i < 6 && fits; i++)
                        {
                            if (yield[i] == 0)
                            {
                                continue;
                            }

                            var ga = Enumerable.Range(0, 4).Sum(k => n[k] * gainA[k][i]);
                            var gb = Enumerable.Range(0, 4).Sum(k => n[k] * gainB[k][i]);
                            fits = ga <= a.Targets[i] && gb <= b.Targets[i];
                            left += a.Targets[i] - ga + b.Targets[i] - gb;
                        }

                        var battles = x1 + x2 + x3 + x4;
                        if (fits && (left < best.Left || (left == best.Left && battles < best.Battles)))
                        {
                            best = (left, battles);
                        }

                        if (!fits)
                        {
                            break;
                        }
                    }
                }
            }
        }

        return best;
    }
}