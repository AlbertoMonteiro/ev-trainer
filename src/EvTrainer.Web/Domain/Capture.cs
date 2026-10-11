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

/// <param name="Score">Sum of Min + Max of the counted ranges; null when the verdict is unavailable.</param>
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

    /// <summary>Every stat counts except the weaker of Ataque and SpAtk (both count on a tie).</summary>
    public static bool[] DefaultCounted(int[] baseStats)
    {
        var counted = Enumerable.Repeat(true, StatVector.Count).ToArray();
        if (baseStats[StatVector.Attack] < baseStats[StatVector.SpAttack])
        {
            counted[StatVector.Attack] = false;
        }
        else if (baseStats[StatVector.SpAttack] < baseStats[StatVector.Attack])
        {
            counted[StatVector.SpAttack] = false;
        }

        return counted;
    }

    /// <summary>Harms when it lowers a stat that counts, helps when it raises one, neutral otherwise.</summary>
    public static NatureEffect EffectOf(Nature? nature, bool[] counted) => nature switch
    {
        { Decreases: { } down } when counted[down] => NatureEffect.Harms,
        { Increases: { } up } when counted[up] => NatureEffect.Helps,
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

    public static CaptureResult Evaluate(CaptureInput input, bool[] counted, Pokedex dex, ReferenceData data)
    {
        var nature = data.FindNature(input.Nature);
        var cells = Ivs.RangeFor(dex.Find(input.Species)?.ToEntry(), nature, input.Level, input.Stats, new int[StatVector.Count]);
        GradeSpan?[] grades = [.. cells.Select(c => c is IvSpan(var min, var max) ? new GradeSpan(Grade(min), Grade(max)) : null)];
        var effective = cells.Select((cell, i) => counted[i] && cell is not IvUnknowable).ToArray();

        var verdict = Judge(cells, effective, out var score);
        return new CaptureResult(cells, grades, verdict, EffectOf(nature, counted), score);
    }

    private static Verdict Judge(IvCell[] cells, bool[] counted, out int? score)
    {
        score = null;
        var relevant = cells.Where((_, i) => counted[i]).ToList();
        if (relevant.Count == 0)
        {
            return new VerdictUnavailable(VerdictProblem.NothingCounted);
        }

        if (relevant.Any(c => c is not IvSpan))
        {
            return new VerdictUnavailable(VerdictProblem.Inconsistent);
        }

        var spans = relevant.Cast<IvSpan>().ToList();
        score = spans.Sum(s => s.Min + s.Max);
        var low = VerdictOf(spans.Sum(s => s.Min) / spans.Count);
        var high = VerdictOf(spans.Sum(s => s.Max) / spans.Count);
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

/// <summary>Per-species capture conveniences kept on the device (outside the backup): counted stats and the best so far.</summary>
public sealed class CaptureNotes
{
    public Dictionary<string, bool[]> Counted { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, CaptureInput> Best { get; init; } = new(StringComparer.Ordinal);

    public CaptureNotes Clone() => new()
    {
        Counted = Counted.ToDictionary(kv => kv.Key, kv => (bool[])[.. kv.Value], StringComparer.Ordinal),
        Best = Best.ToDictionary(kv => kv.Key, kv => kv.Value with { Stats = [.. kv.Value.Stats] }, StringComparer.Ordinal),
    };
}