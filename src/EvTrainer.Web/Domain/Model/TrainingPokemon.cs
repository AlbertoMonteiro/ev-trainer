namespace EvTrainer.Web.Domain.Model;

/// <summary>A Pokémon being trained. Stat vectors are always [HP, Ataque, Defesa, SpAtk, SpDef, Speed].</summary>
public sealed class TrainingPokemon
{
    /// <summary>Species the Pokémon is in right now: "" or the name of one of <see cref="Species"/>.</summary>
    public string CurrentSpecies { get; set; } = "";

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

    /// <summary>Holds the Exp. Share (spec 012); at most one Pokémon in the set.</summary>
    public bool ExpShare { get; set; }

    public int Order { get; set; }

    /// <summary>IVs the user knows from another source (each null or 0–31); they win over the computed range.</summary>
    public int?[] KnownIvs { get; set; } = new int?[StatVector.Count];

    /// <summary>When this Pokémon was last changed on any device (UTC); null = never stamped (older data).</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    public TrainingPokemon Clone() => new()
    {
        CurrentSpecies = CurrentSpecies,
        Nature = Nature,
        Species = [.. Species.Select(s => s.Clone())],
        Targets = [.. Targets],
        Events = [.. Events.Select(e => e.Clone())],
        IvLog = [.. IvLog.Select(l => l.Clone())],
        Pokerus = Pokerus,
        MachoBrace = MachoBrace,
        ExpShare = ExpShare,
        Order = Order,
        KnownIvs = [.. KnownIvs],
        UpdatedAt = UpdatedAt,
    };

    /// <summary>
    /// Inserts a level note keeping <see cref="IvLog"/> sorted by level (stable), and advances the current
    /// species to the one of the note when it is later in the line.
    /// </summary>
    public int InsertLevel(LevelEntry entry)
    {
        var index = IvLog.FindLastIndex(l => l.Level <= entry.Level) + 1;
        IvLog.Insert(index, entry);
        AdvanceTo(entry.Species);
        return index;
    }

    /// <summary>
    /// Makes <paramref name="species"/> the current species when it is in the line and later than the
    /// current one (or nothing is current yet); a Pokémon never goes back to an earlier form.
    /// </summary>
    public void AdvanceTo(string species)
    {
        var target = Species.FindIndex(s => s.Name == species && !string.IsNullOrWhiteSpace(s.Name));
        if (target >= 0 && target > Species.FindIndex(s => s.Name == CurrentSpecies))
        {
            CurrentSpecies = species;
        }
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

    /// <summary>
    /// Manual adjustment: when present, EV totals become exactly these values at this point of the log
    /// (each 0–255, sum ≤ 510); later battles add on top of them.
    /// </summary>
    public int[]? SetTotals { get; set; }

    public bool IsManualAdjustment => SetTotals is not null;

    /// <summary>Battle received through the Exp. Share (spec 012).</summary>
    public bool Shared { get; set; }

    public BattleEvent Clone() => new()
    {
        Opponent = Opponent,
        Count = Count,
        Multiplier = Multiplier,
        Shared = Shared,
        SetTotals = SetTotals is null ? null : [.. SetTotals],
    };
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

    /// <summary>BCP-47 tag of the UI language chosen in the menu; null = follow the browser language (spec 011).</summary>
    public string? Language { get; init; }

    /// <summary>"Where to train" page (spec 013): before | after the Elite Four.</summary>
    public string GuidePhase { get; init; } = "before";

    /// <summary>Game being played (spec 014): fireRed | leafGreen; decides the wild Pokémon of each place.</summary>
    public string GameVersion { get; init; } = GameVersions.FireRedKey;
}