using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>Gen 3 stat formula with integer arithmetic (HANDOFF §2.2).</summary>
public static class StatFormula
{
    /// <param name="natureMultiplier">11 (raised), 9 (lowered) or 10; ignored for HP.</param>
    public static int Value(int @base, int iv, int ev, int level, int natureMultiplier, bool isHp)
    {
        var inner = (2 * @base + iv + ev / 4) * level / 100;
        return isHp ? inner + level + 10 : (inner + 5) * natureMultiplier / 10;
    }
}

/// <summary>IV range of one stat in one level note.</summary>
public closed record IvCell;

public sealed record IvSpan(int Min, int Max) : IvCell;

/// <summary>No IV from 0 to 31 reproduces the shown stat (or the species is not in the line).</summary>
public sealed record IvInconsistent : IvCell;

/// <summary>The stat was not filled in.</summary>
public sealed record IvNoData : IvCell;

/// <summary>Intersection of every valid row for one stat.</summary>
public closed record FinalCell;

public sealed record FinalSpan(int Low, int High) : FinalCell;

public sealed record FinalSingle(int Value) : FinalCell;

/// <summary>Rows don't intersect: some note must be wrong.</summary>
public sealed record FinalConflict : FinalCell;

public sealed record FinalNoData : FinalCell;

/// <summary>IV estimation (HANDOFF §4.7, §4.8).</summary>
public static class Ivs
{
    public static IvCell[] RangeFor(LevelEntry entry, TrainingPokemon pokemon, ReferenceData data)
    {
        var species = pokemon.Species.FirstOrDefault(s => s.Name == entry.Species);
        var nature = data.FindNature(pokemon.Nature);
        var cells = new IvCell[StatVector.Count];
        for (var i = 0; i < StatVector.Count; i++)
        {
            if (entry.Stats[i] <= 0)
            {
                cells[i] = new IvNoData();
                continue;
            }

            if (species is null)
            {
                cells[i] = new IvInconsistent();
                continue;
            }

            int? min = null, max = null;
            for (var iv = 0; iv <= StatVector.MaxIv; iv++)
            {
                var value = StatFormula.Value(species.Base[i], iv, entry.Evs[i], entry.Level, nature?.Multiplier(i) ?? 10, i == StatVector.Hp);
                if (value == entry.Stats[i])
                {
                    min ??= iv;
                    max = iv;
                }
            }

            cells[i] = min is { } low && max is { } high ? new IvSpan(low, high) : new IvInconsistent();
        }

        return cells;
    }

    public static FinalCell[] Final(TrainingPokemon pokemon, ReferenceData data)
    {
        var rows = pokemon.IvLog.Select(entry => RangeFor(entry, pokemon, data)).ToList();
        var final = new FinalCell[StatVector.Count];
        for (var i = 0; i < StatVector.Count; i++)
        {
            var spans = rows.Select(r => r[i]).OfType<IvSpan>().ToList();
            if (spans.Count == 0)
            {
                final[i] = new FinalNoData();
                continue;
            }

            var low = spans.Max(s => s.Min);
            var high = spans.Min(s => s.Max);
            final[i] = low > high ? new FinalConflict() : low == high ? new FinalSingle(low) : new FinalSpan(low, high);
        }

        return final;
    }
}