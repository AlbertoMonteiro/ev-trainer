using System.Globalization;

using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>Lines recognized from a paste, plus how many non-empty lines were ignored.</summary>
public sealed record ImportPreview<T>(IReadOnlyList<T> Items, int Recognized, int Ignored);

/// <summary>Import of rows pasted from a spreadsheet, cells separated by TAB or ";" (HANDOFF §4.9).</summary>
public static class SpreadsheetImport
{
    private static readonly char[] CellSeparators = ['\t', ';'];

    /// <summary>
    /// First cell that is an opponent (or alias) wins; the next cell is the quantity (integer &gt; 0, else 1);
    /// the cell after it equal to "S" means Pokérus (×2).
    /// </summary>
    public static ImportPreview<BattleEvent> Battles(string pasted, ReferenceData data)
    {
        List<BattleEvent> items = [];
        var ignored = 0;
    line: foreach (var cells in Rows(pasted))
        {
            for (var i = 0; i < cells.Length; i++)
            {
                if (data.Lookup(cells[i]) is null)
                {
                    continue;
                }

                items.Add(new BattleEvent
                {
                    Opponent = cells[i],
                    Count = Number(cells, i + 1) is int count and > 0 ? count : 1,
                    Multiplier = Cell(cells, i + 2).Equals("S", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                });
                continue line;
            }

            ignored++;
        }

        return new ImportPreview<BattleEvent>(items, items.Count, ignored);
    }

    /// <summary>
    /// The cell with a species of the evolution line wins; the level is the cell before it, then 6 stats
    /// and up to 6 EVs (missing EVs are 0). Incomplete rows are ignored.
    /// </summary>
    public static ImportPreview<LevelEntry> Levels(string pasted, TrainingPokemon pokemon)
    {
        List<LevelEntry> items = [];
        var ignored = 0;
    line: foreach (var cells in Rows(pasted))
        {
            for (var i = 0; i < cells.Length; i++)
            {
                if (pokemon.Species.FirstOrDefault(s => s.Name.Equals(cells[i], StringComparison.OrdinalIgnoreCase)) is not { } species)
                {
                    continue;
                }

                var level = Number(cells, i - 1);
                int?[] stats = [.. Enumerable.Range(i + 1, StatVector.Count).Select(c => Number(cells, c))];
                if (level is not (>= 1 and <= 100) || stats.Any(s => s is not > 0))
                {
                    break;
                }

                items.Add(new LevelEntry
                {
                    Level = level.Value,
                    Species = species.Name,
                    Stats = [.. stats.Select(s => s!.Value)],
                    Evs = [.. Enumerable.Range(i + 1 + StatVector.Count, StatVector.Count).Select(c => Number(cells, c) ?? 0)],
                });
                continue line;
            }

            ignored++;
        }

        return new ImportPreview<LevelEntry>(items, items.Count, ignored);
    }

    private static IEnumerable<string[]> Rows(string pasted) =>
        pasted.Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(CellSeparators).Select(c => c.Trim()).ToArray());

    private static string Cell(string[] cells, int index) => index >= 0 && index < cells.Length ? cells[index] : string.Empty;

    private static int? Number(string[] cells, int index) =>
        int.TryParse(Cell(cells, index), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
}