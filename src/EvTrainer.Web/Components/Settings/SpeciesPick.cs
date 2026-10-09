using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.Components.Settings;

/// <summary>Evolution line chosen in the picker and the species the user actually tapped.</summary>
public sealed record SpeciesPick(IReadOnlyList<SpeciesEntry> Line, string Picked);