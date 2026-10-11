using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.Domain;

/// <summary>Deduces the current species of data saved when Pokémon still had a free nickname.</summary>
public static class CurrentForm
{
    /// <summary>
    /// The nickname when it names a species of the line, else the most advanced species of the line among
    /// the level notes, else the first named species of the line, else "".
    /// </summary>
    public static string Resolve(string? name, IReadOnlyList<SpeciesEntry> line, IReadOnlyList<LevelEntry> ivLog)
    {
        var named = line.Where(s => !string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Name).ToList();
        if (named.FirstOrDefault(s => s.Equals(name?.Trim(), StringComparison.OrdinalIgnoreCase)) is { } species)
        {
            return species;
        }

        var noted = ivLog.Select(l => named.IndexOf(l.Species)).DefaultIfEmpty(-1).Max();
        return noted >= 0 ? named[noted] : named.FirstOrDefault() ?? "";
    }
}