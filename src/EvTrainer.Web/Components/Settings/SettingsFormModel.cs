using System.Globalization;

using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.Components.Settings;

/// <summary>Editable copy of the settings of a Pokémon, with the validation rules of data-model.md.</summary>
public sealed class SettingsFormModel
{
    public string Name { get; set; } = "";

    public string Nature { get; set; } = "";

    public string[] Targets { get; set; } = [.. Enumerable.Repeat("0", StatVector.Count)];

    /// <summary>Evolution line chosen in the species picker (names and base stats come from the Pokédex).</summary>
    public List<SpeciesEntry> Species { get; set; } = [];

    public static SettingsFormModel From(TrainingPokemon pokemon) => new()
    {
        Name = pokemon.Name,
        Nature = pokemon.Nature,
        Targets = [.. pokemon.Targets.Select(t => t.ToString(CultureInfo.InvariantCulture))],
        Species = [.. pokemon.Species.Where(s => !string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Clone())],
    };

    /// <summary>Sum of the targets that parse; used for the live sum.</summary>
    public int TargetSum => Targets.Sum(t => ParseInRange(t, 0, 255) ?? 0);

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name)
        && Targets.All(t => ParseInRange(t, 0, 255) is not null)
        && Species.Count is >= 1 and <= 3;

    public int[] ParsedTargets => [.. Targets.Select(t => ParseInRange(t, 0, 255) ?? 0)];

    public static int? ParseInRange(string? text, int min, int max) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
        && value >= min && value <= max
            ? value
            : null;
}