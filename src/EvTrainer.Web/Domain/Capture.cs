using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>What the player types after catching a Pokémon (its EVs are zero in Gen 3).</summary>
public sealed record CaptureInput(string Species, int Level, string Nature, int[] Stats)
{
    public const int MaxStat = 999;

    /// <summary>Species in the Pokédex, level 1–100, a known nature and six stats of 1–999.</summary>
    public bool IsValid(Pokedex dex, ReferenceData data) =>
        dex.Find(Species) is not null
        && Level is >= 1 and <= 100
        && data.FindNature(Nature) is not null
        && Stats.Length == StatVector.Count
        && Stats.All(s => s is >= 1 and <= MaxStat);
}

/// <summary>Grade of one IV: Ruim 0–10, Médio 11–19, Bom 20–25, Ótimo 26–31.</summary>
public enum IvGrade
{
    Poor,
    Fair,
    Good,
    Great,
}

/// <summary>Grades of both ends of an IV range ("Médio a Bom" when they differ).</summary>
public sealed record GradeSpan(IvGrade Low, IvGrade High);

public enum VerdictGrade
{
    Weak,
    Fair,
    Good,
    Excellent,
}

public enum VerdictProblem
{
    NothingCounted,
    Inconsistent,
}

/// <summary>Overall verdict of a capture over the stats that count.</summary>
public closed record Verdict;

public sealed record VerdictUnavailable(VerdictProblem Reason) : Verdict;

public sealed record VerdictKnown(VerdictGrade Grade) : Verdict;

/// <summary>The ranges are too wide: the average of the lows and of the highs fall in different grades.</summary>
public sealed record VerdictUncertain(VerdictGrade Low, VerdictGrade High) : Verdict;

/// <summary>How much a stat weighs in the verdict and the comparison; the value is the weight (amendment R10).</summary>
public enum StatImportance
{
    Ignored = 0,
    Counts = 1,
    Main = 3,
}

/// <summary>Effect of the nature on the stats that count; ordered from worst to best.</summary>
public enum NatureEffect
{
    Harms,
    Neutral,
    Helps,
}

public enum CaptureComparison
{
    Better,
    Equivalent,
    Worse,
}

/// <param name="Score">Sum of weight × (Min + Max) of the weighted ranges; null when the verdict is unavailable.</param>
public sealed record CaptureResult(IvCell[] Cells, GradeSpan?[] Grades, Verdict Verdict, NatureEffect NatureEffect, int? Score);

/// <summary>Judges a freshly caught Pokémon (spec 005, research R4–R6).</summary>
public static class Capture
{
    public static IvGrade Grade(int iv) => iv switch
    {
        <= 10 => IvGrade.Poor,
        <= 19 => IvGrade.Fair,
        <= 25 => IvGrade.Good,
        _ => IvGrade.Great,
    };

    /// <summary>
    /// The stronger of Ataque and SpAtk is the main stat, the weaker is ignored, the others count (on a tie
    /// both attack stats count).
    /// </summary>
    public static StatImportance[] DefaultImportance(int[] baseStats)
    {
        var importance = Enumerable.Repeat(StatImportance.Counts, StatVector.Count).ToArray();
        var (attack, spAttack) = (baseStats[StatVector.Attack], baseStats[StatVector.SpAttack]);
        if (attack != spAttack)
        {
            var (main, weaker) = attack > spAttack ? (StatVector.Attack, StatVector.SpAttack) : (StatVector.SpAttack, StatVector.Attack);
            importance[main] = StatImportance.Main;
            importance[weaker] = StatImportance.Ignored;
        }

        return importance;
    }

    /// <summary>One tap: ignored → counts → main → ignored.</summary>
    public static StatImportance Next(StatImportance importance) => importance switch
    {
        StatImportance.Ignored => StatImportance.Counts,
        StatImportance.Counts => StatImportance.Main,
        _ => StatImportance.Ignored,
    };

    /// <summary>Harms when it lowers a stat that counts, helps when it raises one, neutral otherwise.</summary>
    public static NatureEffect EffectOf(Nature? nature, StatImportance[] importance) => nature switch
    {
        { Decreases: { } down } when importance[down] != StatImportance.Ignored => NatureEffect.Harms,
        { Increases: { } up } when importance[up] != StatImportance.Ignored => NatureEffect.Helps,
        _ => NatureEffect.Neutral,
    };

    /// <summary>Score difference up to which two captures tie (2 points of the doubled score).</summary>
    public const int TieMargin = 4;

    /// <summary>
    /// Higher score wins; within <see cref="TieMargin"/> the better nature effect wins, otherwise they are
    /// equivalent (research R6). Both results must have a score.
    /// </summary>
    public static CaptureComparison Compare(CaptureResult current, CaptureResult best)
    {
        var diff = (current.Score ?? 0) - (best.Score ?? 0);
        var order = Math.Abs(diff) > TieMargin ? diff : current.NatureEffect.CompareTo(best.NatureEffect);
        return order switch
        {
            > 0 => CaptureComparison.Better,
            < 0 => CaptureComparison.Worse,
            _ => CaptureComparison.Equivalent,
        };
    }

    public static CaptureResult Evaluate(CaptureInput input, StatImportance[] importance, Pokedex dex, ReferenceData data)
    {
        var nature = data.FindNature(input.Nature);
        var cells = Ivs.RangeFor(dex.Find(input.Species)?.ToEntry(), nature, input.Level, input.Stats, new int[StatVector.Count]);
        GradeSpan?[] grades = [.. cells.Select(c => c is IvSpan(var min, var max) ? new GradeSpan(Grade(min), Grade(max)) : null)];
        var weights = cells.Select((cell, i) => cell is IvUnknowable ? 0 : (int)importance[i]).ToArray();

        var verdict = Judge(cells, weights, out var score);
        return new CaptureResult(cells, grades, verdict, EffectOf(nature, importance), score);
    }

    /// <summary>Weighted averages of the lows and of the highs of the weighted stats (research R4, R10).</summary>
    private static Verdict Judge(IvCell[] cells, int[] weights, out int? score)
    {
        score = null;
        var relevant = cells.Select((cell, i) => (Cell: cell, Weight: weights[i])).Where(x => x.Weight > 0).ToList();
        if (relevant.Count == 0)
        {
            return new VerdictUnavailable(VerdictProblem.NothingCounted);
        }

        if (relevant.Any(x => x.Cell is not IvSpan))
        {
            return new VerdictUnavailable(VerdictProblem.Inconsistent);
        }

        var spans = relevant.Select(x => (Span: (IvSpan)x.Cell, x.Weight)).ToList();
        var total = spans.Sum(x => x.Weight);
        score = spans.Sum(x => x.Weight * (x.Span.Min + x.Span.Max));
        var low = VerdictOf(spans.Sum(x => x.Weight * x.Span.Min) / total);
        var high = VerdictOf(spans.Sum(x => x.Weight * x.Span.Max) / total);
        return low == high ? new VerdictKnown(low) : new VerdictUncertain(low, high);
    }

    private static VerdictGrade VerdictOf(int averageIv) => averageIv switch
    {
        < 11 => VerdictGrade.Weak,
        < 20 => VerdictGrade.Fair,
        < 26 => VerdictGrade.Good,
        _ => VerdictGrade.Excellent,
    };
}

/// <summary>Per-species capture conveniences kept on the device (outside the backup): stat importance and the best so far.</summary>
public sealed class CaptureNotes
{
    public Dictionary<string, StatImportance[]> Importance { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, CaptureInput> Best { get; init; } = new(StringComparer.Ordinal);

    public CaptureNotes Clone() => new()
    {
        Importance = Importance.ToDictionary(kv => kv.Key, kv => (StatImportance[])[.. kv.Value], StringComparer.Ordinal),
        Best = Best.ToDictionary(kv => kv.Key, kv => kv.Value with { Stats = [.. kv.Value.Stats] }, StringComparer.Ordinal),
    };
}