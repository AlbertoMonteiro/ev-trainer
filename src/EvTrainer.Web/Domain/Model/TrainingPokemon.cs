namespace EvTrainer.Web.Domain.Model;

/// <summary>A Pokémon being trained. Stat vectors are always [HP, Ataque, Defesa, SpAtk, SpDef, Speed].</summary>
public sealed class TrainingPokemon
{
    public string Name { get; set; } = "";

    /// <summary>Nature name, or "" when not chosen (treated as neutral).</summary>
    public string Nature { get; set; } = "";

    /// <summary>Evolution line, 1 to 3 entries, in order.</summary>
    public List<SpeciesEntry> Species { get; set; } = [];

    /// <summary>EV targets, each 0–255. A sum above 510 is allowed (with a warning).</summary>
    public int[] Targets { get; set; } = new int[StatVector.Count];

    /// <summary>Ordered battle log. Order is significant; never reorder.</summary>
    public List<BattleEvent> Events { get; set; } = [];

    /// <summary>Level notes, kept sorted by level (stable).</summary>
    public List<LevelEntry> IvLog { get; set; } = [];

    public bool Pokerus { get; set; }

    public bool MachoBrace { get; set; }

    public int Order { get; set; }

    /// <summary>
    /// Species to picture this Pokémon with: the one of the latest level note (when it is in the
    /// evolution line), else the first species of the line, else the nickname.
    /// </summary>
    public string DisplaySpecies =>
        IvLog.LastOrDefault()?.Species is { } latest && Species.Any(s => s.Name == latest) ? latest
        : Species.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Name))?.Name ?? Name;

    public TrainingPokemon Clone() => new()
    {
        Name = Name,
        Nature = Nature,
        Species = [.. Species.Select(s => s.Clone())],
        Targets = [.. Targets],
        Events = [.. Events.Select(e => e.Clone())],
        IvLog = [.. IvLog.Select(l => l.Clone())],
        Pokerus = Pokerus,
        MachoBrace = MachoBrace,
        Order = Order,
    };

    /// <summary>Inserts a level note keeping <see cref="IvLog"/> sorted by level (stable).</summary>
    public int InsertLevel(LevelEntry entry)
    {
        var index = IvLog.FindLastIndex(l => l.Level <= entry.Level) + 1;
        IvLog.Insert(index, entry);
        return index;
    }
}

public sealed class SpeciesEntry
{
    public string Name { get; set; } = "";

    /// <summary>Base stats, each 1–255.</summary>
    public int[] Base { get; set; } = new int[StatVector.Count];

    public SpeciesEntry Clone() => new() { Name = Name, Base = [.. Base] };
}

public sealed class BattleEvent
{
    /// <summary>Opponent or vitamin name as recorded (may be an alias or unknown).</summary>
    public string Opponent { get; set; } = "";

    /// <summary>How many in a row; at least 1.</summary>
    public int Count { get; set; } = 1;

    /// <summary>1, 2 or 4 at the time of the battle. Ignored for vitamins.</summary>
    public int Multiplier { get; set; } = 1;

    public BattleEvent Clone() => new() { Opponent = Opponent, Count = Count, Multiplier = Multiplier };
}

public sealed class LevelEntry
{
    /// <summary>Level, 1–100.</summary>
    public int Level { get; set; }

    /// <summary>Species at that level; should be one of the evolution line names.</summary>
    public string Species { get; set; } = "";

    /// <summary>Stats shown on screen.</summary>
    public int[] Stats { get; set; } = new int[StatVector.Count];

    /// <summary>EVs at that moment, 0–255 each.</summary>
    public int[] Evs { get; set; } = new int[StatVector.Count];

    public LevelEntry Clone() => new() { Level = Level, Species = Species, Stats = [.. Stats], Evs = [.. Evs] };
}

/// <summary>Per-device UI preferences, kept outside the backup.</summary>
public sealed record UiPreferences
{
    public string? SelectedId { get; init; }

    /// <summary>system | light | dark.</summary>
    public string Theme { get; init; } = "system";

    /// <summary>useful | all.</summary>
    public string Filter { get; init; } = "useful";

    /// <summary>BCP-47 tag of the UI language.</summary>
    public string Language { get; init; } = "pt-BR";
}