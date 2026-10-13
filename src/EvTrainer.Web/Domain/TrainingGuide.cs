using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>A trainer on the "where to train" page: its stages visible in the phase and the best one for the stat.</summary>
public sealed record GuideEntry(GuideTrainer Trainer, IReadOnlyList<GuideStage> VisibleStages, GuideStage Best, int Score);

/// <summary>
/// Ranks the VS Seeker trainers by the EVs they give (spec 013). Each trainer is its own entry: the VS Seeker only
/// wakes up the trainers on screen, so trainers are never added together.
/// </summary>
public sealed class TrainingGuide(VsSeekerGuide guide, Pokedex dex)
{
    public const string Before = "before";
    public const string After = "after";

    /// <summary>Stats the page offers: HP, Attack, Sp. Atk and Speed.</summary>
    public static readonly int[] Stats = [0, 1, 3, 5];

    /// <summary>EVs of one battle against the stage's team (sum of the species yields).</summary>
    public int[] StageEvs(GuideStage stage)
    {
        var evs = new int[StatVector.Count];
        foreach (var (species, _) in stage.Team)
        {
            if (dex.Find(species) is { } found)
            {
                for (var i = 0; i < StatVector.Count; i++)
                {
                    evs[i] += found.EvYield[i];
                }
            }
        }

        return evs;
    }

    /// <summary>Stages reachable in the phase: before the Elite Four hides stages 4 and 5.</summary>
    public static IReadOnlyList<GuideStage> Visible(GuideTrainer trainer, string phase) =>
        phase == After ? trainer.Stages : [.. trainer.Stages.Where(s => !s.RequiresEliteFour)];

    /// <summary>
    /// Trainers with a visible stage giving <paramref name="stat"/>, by that stat's EVs in their best visible stage
    /// (highest first; ties: lower team level first, then the game's order).
    /// </summary>
    public IReadOnlyList<GuideEntry> Rank(int stat, string phase) =>
    [
        .. guide.Trainers
            .Select((trainer, order) => (trainer, order, visible: Visible(trainer, phase)))
            .Select(x => (x.trainer, x.order, x.visible, best: x.visible
                .OrderByDescending(s => StageEvs(s)[stat])
                .ThenBy(s => s.Team.Max(p => p.Level))
                .FirstOrDefault()))
            .Where(x => x.best is not null && StageEvs(x.best)[stat] > 0)
            .OrderByDescending(x => StageEvs(x.best!)[stat])
            .ThenBy(x => x.best!.Team.Max(p => p.Level))
            .ThenBy(x => x.order)
            .Select(x => new GuideEntry(x.trainer, x.visible, x.best!, StageEvs(x.best!)[stat])),
    ];

    /// <summary>The offered stat whose target is missing the most EVs; HP when no target is missing anything.</summary>
    public static int InitialStat(int[] totals, int[] targets)
    {
        var best = Stats[0];
        var missing = 0;
        foreach (var stat in Stats)
        {
            if (targets[stat] - totals[stat] > missing)
            {
                missing = targets[stat] - totals[stat];
                best = stat;
            }
        }

        return best;
    }

    /// <summary>Stats the stage gives whose target is already met (a target of 0 counts as met): those EVs are wasted.</summary>
    public IReadOnlyList<int> WastedStats(GuideStage stage, int[] totals, int[] targets)
    {
        var evs = StageEvs(stage);
        return [.. Enumerable.Range(0, StatVector.Count).Where(i => evs[i] > 0 && totals[i] >= targets[i])];
    }
}