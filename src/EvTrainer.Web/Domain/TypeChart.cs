namespace EvTrainer.Web.Domain;

/// <summary>
/// Generation III type chart (spec 015). In Gen III a move is physical or special by its type alone: Normal, Fighting,
/// Flying, Poison, Ground, Rock, Bug, Ghost and Steel are physical, the rest special.
/// </summary>
public static class TypeChart
{
    public static readonly IReadOnlyList<string> Types =
    [
        "Normal", "Fire", "Water", "Electric", "Grass", "Ice", "Fighting", "Poison", "Ground",
        "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel",
    ];

    private static readonly HashSet<string> Physical =
        ["Normal", "Fighting", "Flying", "Poison", "Ground", "Rock", "Bug", "Ghost", "Steel"];

    /// <summary>Attack type → defender type → multiplier, only for the entries that are not 1×.</summary>
    private static readonly Dictionary<string, Dictionary<string, double>> Chart = new()
    {
        ["Normal"] = new() { ["Rock"] = 0.5, ["Ghost"] = 0, ["Steel"] = 0.5 },
        ["Fire"] = new() { ["Fire"] = 0.5, ["Water"] = 0.5, ["Grass"] = 2, ["Ice"] = 2, ["Bug"] = 2, ["Rock"] = 0.5, ["Dragon"] = 0.5, ["Steel"] = 2 },
        ["Water"] = new() { ["Fire"] = 2, ["Water"] = 0.5, ["Grass"] = 0.5, ["Ground"] = 2, ["Rock"] = 2, ["Dragon"] = 0.5 },
        ["Electric"] = new() { ["Water"] = 2, ["Electric"] = 0.5, ["Grass"] = 0.5, ["Ground"] = 0, ["Flying"] = 2, ["Dragon"] = 0.5 },
        ["Grass"] = new() { ["Fire"] = 0.5, ["Water"] = 2, ["Grass"] = 0.5, ["Poison"] = 0.5, ["Ground"] = 2, ["Flying"] = 0.5, ["Bug"] = 0.5, ["Rock"] = 2, ["Dragon"] = 0.5, ["Steel"] = 0.5 },
        ["Ice"] = new() { ["Fire"] = 0.5, ["Water"] = 0.5, ["Grass"] = 2, ["Ice"] = 0.5, ["Ground"] = 2, ["Flying"] = 2, ["Dragon"] = 2, ["Steel"] = 0.5 },
        ["Fighting"] = new() { ["Normal"] = 2, ["Ice"] = 2, ["Poison"] = 0.5, ["Flying"] = 0.5, ["Psychic"] = 0.5, ["Bug"] = 0.5, ["Rock"] = 2, ["Ghost"] = 0, ["Dark"] = 2, ["Steel"] = 2 },
        ["Poison"] = new() { ["Grass"] = 2, ["Poison"] = 0.5, ["Ground"] = 0.5, ["Rock"] = 0.5, ["Ghost"] = 0.5, ["Steel"] = 0 },
        ["Ground"] = new() { ["Fire"] = 2, ["Electric"] = 2, ["Grass"] = 0.5, ["Poison"] = 2, ["Flying"] = 0, ["Bug"] = 0.5, ["Rock"] = 2, ["Steel"] = 2 },
        ["Flying"] = new() { ["Electric"] = 0.5, ["Grass"] = 2, ["Fighting"] = 2, ["Bug"] = 2, ["Rock"] = 0.5, ["Steel"] = 0.5 },
        ["Psychic"] = new() { ["Fighting"] = 2, ["Poison"] = 2, ["Psychic"] = 0.5, ["Dark"] = 0, ["Steel"] = 0.5 },
        ["Bug"] = new() { ["Fire"] = 0.5, ["Grass"] = 2, ["Fighting"] = 0.5, ["Poison"] = 0.5, ["Flying"] = 0.5, ["Psychic"] = 2, ["Ghost"] = 0.5, ["Dark"] = 2, ["Steel"] = 0.5 },
        ["Rock"] = new() { ["Fire"] = 2, ["Ice"] = 2, ["Fighting"] = 0.5, ["Ground"] = 0.5, ["Flying"] = 2, ["Bug"] = 2, ["Steel"] = 0.5 },
        ["Ghost"] = new() { ["Normal"] = 0, ["Psychic"] = 2, ["Ghost"] = 2, ["Dark"] = 0.5, ["Steel"] = 0.5 },
        ["Dragon"] = new() { ["Dragon"] = 2, ["Steel"] = 0.5 },
        ["Dark"] = new() { ["Fighting"] = 0.5, ["Psychic"] = 2, ["Ghost"] = 2, ["Dark"] = 0.5, ["Steel"] = 0.5 },
        ["Steel"] = new() { ["Fire"] = 0.5, ["Water"] = 0.5, ["Electric"] = 0.5, ["Ice"] = 2, ["Rock"] = 2, ["Steel"] = 0.5 },
    };

    /// <summary>Product of the multipliers against each of the defender's types (0, ¼, ½, 1, 2 or 4).</summary>
    public static double Multiplier(string attackType, IEnumerable<string> defenderTypes) =>
        defenderTypes.Aggregate(1.0, (total, defender) =>
            total * (Chart.TryGetValue(attackType, out var row) && row.TryGetValue(defender, out var value) ? value : 1));

    public static bool IsPhysical(string type) => Physical.Contains(type);
}