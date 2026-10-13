using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.State;

public enum SaveStatus
{
    Saved,
    Saving,
    Failed,
}

/// <summary>
/// In-memory state of every trained Pokémon. Applies domain commands, saves immediately through
/// <see cref="IPokemonRepository"/> and raises <see cref="Changed"/>. Every command that changes a Pokémon
/// stamps it (<see cref="TrainingPokemon.UpdatedAt"/>) and every removal is recorded, for the sync (spec 008).
/// Holds no UI text.
/// </summary>
public sealed partial class TrainerStore(IPokemonRepository repository, ReferenceData data, TimeProvider? time = null)
{
    private readonly Dictionary<string, TrainingPokemon> _all = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _deleted = new(StringComparer.Ordinal);
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public event Action? Changed;

    /// <summary>Raised after a local edit is saved (not after preferences or applying a sync).</summary>
    public event Action? LocalDataChanged;

    public ReferenceData Data => data;

    public SaveStatus Status { get; private set; } = SaveStatus.Saved;

    public UiPreferences Preferences { get; private set; } = new();

    public string? SelectedId => Preferences.SelectedId is { } id && _all.ContainsKey(id) ? id : null;

    public TrainingPokemon? Selected => SelectedId is { } id ? _all[id] : null;

    public IReadOnlyList<(string Id, TrainingPokemon Pokemon)> Ordered =>
        [.. _all.OrderBy(kv => kv.Value.Order).ThenBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => (kv.Key, kv.Value))];

    public IReadOnlyDictionary<string, TrainingPokemon> All => _all;

    public void Load()
    {
        _all.Clear();
        foreach (var (id, pokemon) in repository.LoadAll())
        {
            _all[id] = pokemon;
        }

        _deleted.Clear();
        foreach (var (id, at) in repository.LoadDeleted())
        {
            _deleted[id] = at;
        }

        if (KeepOneExpShareHolder(preferred: null) is { Length: > 0 })
        {
            Save();
        }

        Preferences = repository.LoadPreferences();
        if (SelectedId is null && Ordered.Count > 0)
        {
            Preferences = Preferences with { SelectedId = Ordered[0].Id };
        }
    }

    /// <summary>Creates a blank Pokémon (one unnamed species, targets 0), selects it and saves.</summary>
    public string AddPokemon()
    {
        var id = Guid.CreateVersion7().ToString("N");
        _all[id] = new TrainingPokemon
        {
            Species = [new SpeciesEntry()],
            Order = (_all.Count == 0 ? 0 : _all.Values.Max(p => p.Order)) + 1,
        };
        UpdatePreferences(Preferences with { SelectedId = id });
        Commit(id);
        return id;
    }

    /// <summary>
    /// Creates a Pokémon from an evaluated capture (research R9): current form = the captured species, evolution line from the
    /// Pokédex, the nature, targets zero and the evaluated level as its first IV note (EVs zero). Selects it;
    /// the undo removes it and restores the previous selection.
    /// </summary>
    public (string Id, Action Undo) AddFromCapture(CaptureInput input, Pokedex dex)
    {
        var previous = Preferences.SelectedId;
        var id = Guid.CreateVersion7().ToString("N");
        var species = dex.Find(input.Species);
        var name = species?.Name ?? input.Species;
        List<SpeciesEntry> line = species is null ? [new SpeciesEntry { Name = name }] : [.. dex.LineFor(species).Select(s => s.ToEntry())];
        _all[id] = new TrainingPokemon
        {
            CurrentSpecies = name,
            Nature = input.Nature,
            Species = line,
            Order = (_all.Count == 0 ? 0 : _all.Values.Max(p => p.Order)) + 1,
            IvLog =
            [
                new LevelEntry
                {
                    Level = input.Level,
                    Species = name,
                    Stats = [.. input.Stats],
                    Evs = new int[StatVector.Count],
                },
            ],
        };
        UpdatePreferences(Preferences with { SelectedId = id });
        Commit(id);
        return (id, () =>
        {
            if (Forget(id))
            {
                UpdatePreferences(Preferences with { SelectedId = previous is not null && _all.ContainsKey(previous) ? previous : Ordered.FirstOrDefault().Id });
                Commit();
            }
        }
        );
    }

    /// <summary>The Pokémon holding the Exp. Share (spec 012), if any.</summary>
    public string? ExpShareHolderId => _all.FirstOrDefault(kv => kv.Value.ExpShare).Key;

    /// <summary>Gives the Exp. Share to <paramref name="id"/> (taking it from anyone else); null takes it away.</summary>
    public void SetExpShare(string? id)
    {
        List<string> changed = [];
        foreach (var (key, pokemon) in _all)
        {
            var holds = key == id;
            if (pokemon.ExpShare != holds)
            {
                pokemon.ExpShare = holds;
                changed.Add(key);
            }
        }

        Commit([.. changed]);
    }

    /// <summary>
    /// Records a battle for the selected Pokémon and, when another Pokémon holds the Exp. Share and the opponent
    /// is not a vitamin, for the holder too (spec 012); null when nothing is selected.
    /// </summary>
    public SharedRecord? RecordBattle(string opponent)
    {
        if (SelectedId is not { } id || Selected is not { } pokemon)
        {
            return null;
        }

        var main = BattleLog.Record(pokemon, opponent, data);
        if (ExpShareHolderId is { } holderId && holderId != id && data.Lookup(opponent) is not { IsVitamin: true })
        {
            var holder = BattleLog.RecordShared(_all[holderId], opponent, data);
            Commit(id, holderId);
            return new SharedRecord(id, opponent, main, holderId, holder);
        }

        Commit(id);
        return new SharedRecord(id, opponent, main, null, null);
    }

    /// <summary>Undoes a recorded battle on the selected Pokémon and, if it was shared, on the Exp. Share holder.</summary>
    public void UndoBattle(SharedRecord record)
    {
        if (_all.TryGetValue(record.MainId, out var main))
        {
            BattleLog.UndoLast(main);
        }

        if (record.HolderId is { } holderId && _all.TryGetValue(holderId, out var holder))
        {
            BattleLog.UndoLastShared(holder, record.Opponent, data);
            Commit(record.MainId, holderId);
            return;
        }

        Commit(record.MainId);
    }

    public void TogglePokerus()
    {
        if (Selected is { } pokemon)
        {
            pokemon.Pokerus = !pokemon.Pokerus;
            Commit(SelectedId!);
        }
    }

    public void ToggleMacho()
    {
        if (Selected is { } pokemon)
        {
            pokemon.MachoBrace = !pokemon.MachoBrace;
            Commit(SelectedId!);
        }
    }

    public void SaveSettings(string current, string nature, int[] targets, IReadOnlyList<SpeciesEntry> species)
    {
        if (Selected is not { } pokemon || !species.Any(s => s.Name == current))
        {
            return;
        }

        pokemon.CurrentSpecies = current;
        pokemon.Nature = nature;
        pokemon.Targets = targets.ToSix();
        pokemon.Species = [.. species.Select(s => s.Clone())];
        Commit(SelectedId!);
    }

    /// <summary>Removes one event of the selected Pokémon; the returned undo puts it back at the same index.</summary>
    public Action? RemoveEvent(int index)
    {
        if (SelectedId is not { } id || index < 0 || index >= _all[id].Events.Count)
        {
            return null;
        }

        var removed = _all[id].Events[index];
        _all[id].Events.RemoveAt(index);
        Commit(id);
        return () =>
        {
            if (_all.TryGetValue(id, out var pokemon))
            {
                pokemon.Events.Insert(Math.Min(index, pokemon.Events.Count), removed);
                Commit(id);
            }
        };
    }

    /// <summary>
    /// Appends a manual adjustment that fixes the selected Pokémon's EV totals at <paramref name="targets"/>;
    /// null when the values are invalid or already the current totals. The undo removes the adjustment.
    /// </summary>
    public Action? AdjustEvs(int[] targets)
    {
        if (SelectedId is not { } id || !IsValidEvSet(targets) || targets.SequenceEqual(SelectedTotals))
        {
            return null;
        }

        var adjustment = new BattleEvent { Opponent = "", SetTotals = [.. targets] };
        _all[id].Events.Add(adjustment);
        Commit(id);
        return () =>
        {
            if (_all.TryGetValue(id, out var pokemon) && pokemon.Events.Remove(adjustment))
            {
                Commit(id);
            }
        };
    }

    /// <summary>
    /// Replaces the selected Pokémon's battle log with a single adjustment at <paramref name="targets"/>
    /// (level notes are kept); the undo restores the previous log.
    /// </summary>
    public Action? ResetHistory(int[] targets)
    {
        if (SelectedId is not { } id || !IsValidEvSet(targets))
        {
            return null;
        }

        var previous = _all[id].Events;
        _all[id].Events = [new BattleEvent { Opponent = "", SetTotals = [.. targets] }];
        Commit(id);
        return () =>
        {
            if (_all.TryGetValue(id, out var pokemon))
            {
                pokemon.Events = previous;
                Commit(id);
            }
        };
    }

    /// <summary>Six values, each 0–255, summing at most 510.</summary>
    public static bool IsValidEvSet(int[] values) =>
        values.Length == StatVector.Count
        && values.All(v => v is >= 0 and <= StatVector.MaxEvPerStat)
        && values.Sum() <= StatVector.MaxEvTotal;

    /// <summary>Deletes a Pokémon (selecting the first remaining one); the returned undo restores it.</summary>
    public Action? DeletePokemon(string id)
    {
        if (!_all.TryGetValue(id, out var removed))
        {
            return null;
        }

        Forget(id);

        if (Preferences.SelectedId == id)
        {
            UpdatePreferences(Preferences with { SelectedId = Ordered.FirstOrDefault().Id });
        }

        Commit();
        return () =>
        {
            _deleted.Remove(id);
            _all[id] = removed;
            UpdatePreferences(Preferences with { SelectedId = id });
            Commit(id);
        };
    }

    /// <summary>Appends imported battles to the end of the log.</summary>
    public void ImportBattles(IReadOnlyList<BattleEvent> events)
    {
        if (Selected is { } pokemon)
        {
            pokemon.Events.AddRange(events.Select(e => e.Clone()));
            Commit(SelectedId!);
        }
    }

    /// <summary>Inserts imported level notes keeping the log sorted by level.</summary>
    public void ImportLevels(IReadOnlyList<LevelEntry> levels)
    {
        if (Selected is { } pokemon)
        {
            foreach (var level in levels)
            {
                pokemon.InsertLevel(level.Clone());
            }

            Commit(SelectedId!);
        }
    }

    /// <summary>Adds a level note keeping the log sorted by level.</summary>
    public void AddLevel(LevelEntry entry)
    {
        if (Selected is { } pokemon)
        {
            pokemon.InsertLevel(entry.Clone());
            Commit(SelectedId!);
        }
    }

    /// <summary>
    /// Replaces the level note at <paramref name="index"/> (re-sorting by level when it changes); the undo
    /// puts the original note back at its original position.
    /// </summary>
    public Action? ReplaceLevel(int index, LevelEntry entry)
    {
        if (SelectedId is not { } id || index < 0 || index >= _all[id].IvLog.Count)
        {
            return null;
        }

        var original = _all[id].IvLog[index];
        var originalSpecies = _all[id].CurrentSpecies;
        var replacement = entry.Clone();
        _all[id].IvLog.RemoveAt(index);
        _all[id].InsertLevel(replacement);
        Commit(id);
        return () =>
        {
            if (_all.TryGetValue(id, out var pokemon) && pokemon.IvLog.Remove(replacement))
            {
                pokemon.IvLog.Insert(Math.Min(index, pokemon.IvLog.Count), original);
                pokemon.CurrentSpecies = originalSpecies;
                Commit(id);
            }
        };
    }

    /// <summary>Removes a level note and returns an undo action that puts it back at the same index.</summary>
    public Action? RemoveLevel(int index)
    {
        if (SelectedId is not { } id || index < 0 || index >= _all[id].IvLog.Count)
        {
            return null;
        }

        var removed = _all[id].IvLog[index];
        _all[id].IvLog.RemoveAt(index);
        Commit(id);
        return () =>
        {
            if (_all.TryGetValue(id, out var pokemon))
            {
                pokemon.IvLog.Insert(Math.Min(index, pokemon.IvLog.Count), removed);
                Commit(id);
            }
        };
    }

    /// <summary>Saves the IVs the player knows (null = unknown, otherwise 0–31); the undo restores the old ones.</summary>
    public Action? SetKnownIvs(int?[] ivs)
    {
        if (SelectedId is not { } id
            || ivs.Length != StatVector.Count
            || ivs.Any(v => v is < 0 or > StatVector.MaxIv))
        {
            return null;
        }

        var previous = _all[id].KnownIvs;
        _all[id].KnownIvs = [.. ivs];
        Commit(id);
        return () =>
        {
            if (_all.TryGetValue(id, out var pokemon))
            {
                pokemon.KnownIvs = previous;
                Commit(id);
            }
        };
    }

    public string ExportBackup() => BackupFormat.Write(_all, indented: true);

    /// <summary>Merges a backup by id: same ids are replaced, new ones added, local ones kept.</summary>
    public BackupMergePreview ImportBackup(BackupDocuments backup)
    {
        var preview = BackupFormat.Preview(_all, backup.Items);
        foreach (var (id, pokemon) in backup.Items)
        {
            _all[id] = pokemon.Clone();
            _deleted.Remove(id);
        }

        // The holder in the imported file is an explicit choice, so it wins over the local one.
        var unmarked = KeepOneExpShareHolder(preferred: backup.Items.FirstOrDefault(kv => kv.Value.ExpShare).Key);

        if (SelectedId is null && Ordered.Count > 0)
        {
            UpdatePreferences(Preferences with { SelectedId = backup.Items.Keys.FirstOrDefault() ?? Ordered[0].Id });
        }

        Commit([.. backup.Items.Keys, .. unmarked]);
        return preview;
    }

    /// <summary>Current EV totals of the selected Pokémon.</summary>
    public int[] SelectedTotals => Selected is { } pokemon ? Evs.Totals(pokemon.Events, data) : new int[StatVector.Count];

    public void Select(string id)
    {
        if (_all.ContainsKey(id))
        {
            UpdatePreferences(Preferences with { SelectedId = id });
            NotifyChanged();
        }
    }

    public void SetFilter(string filter)
    {
        UpdatePreferences(Preferences with { Filter = filter });
        NotifyChanged();
    }

    public void SetTheme(string theme)
    {
        UpdatePreferences(Preferences with { Theme = theme });
        NotifyChanged();
    }

    public void SetGuidePhase(string phase)
    {
        UpdatePreferences(Preferences with { GuidePhase = phase });
        NotifyChanged();
    }

    public void SetLanguage(string language)
    {
        UpdatePreferences(Preferences with { Language = language });
        NotifyChanged();
    }

    /// <summary>Copy of everything the sync exchanges: the Pokémon and the deletion records.</summary>
    public SyncSnapshot Snapshot() => new(
        _all.ToDictionary(kv => kv.Key, kv => kv.Value.Clone(), StringComparer.Ordinal),
        new Dictionary<string, DateTimeOffset>(_deleted, StringComparer.Ordinal));

    /// <summary>
    /// Replaces the data with the result of a sync merge, without stamping (the stamps come with the merge) and
    /// without raising <see cref="LocalDataChanged"/>. Keeps the selection when the Pokémon still exists.
    /// </summary>
    public void ApplySync(MergeResult result)
    {
        _all.Clear();
        foreach (var (id, pokemon) in result.Documents)
        {
            _all[id] = pokemon.Clone();
        }

        _deleted.Clear();
        foreach (var (id, at) in result.Deleted)
        {
            _deleted[id] = at;
        }

        KeepOneExpShareHolder(preferred: null);

        if (SelectedId is null)
        {
            UpdatePreferences(Preferences with { SelectedId = Ordered.FirstOrDefault().Id });
        }

        Save();
        NotifyChanged();
    }

    /// <summary>
    /// With more than one Exp. Share holder (a sync or an import), keeps <paramref name="preferred"/> or else the
    /// most recently changed one (tie: lowest order); returns the ids that lost it.
    /// </summary>
    private string[] KeepOneExpShareHolder(string? preferred)
    {
        var holders = _all.Where(kv => kv.Value.ExpShare).ToList();
        if (holders.Count <= 1)
        {
            return [];
        }

        var keep = preferred is not null && holders.Any(h => h.Key == preferred)
            ? preferred
            : holders.OrderByDescending(h => h.Value.UpdatedAt ?? DateTimeOffset.MinValue).ThenBy(h => h.Value.Order).First().Key;
        string[] losers = [.. holders.Where(h => h.Key != keep).Select(h => h.Key)];
        foreach (var id in losers)
        {
            _all[id].ExpShare = false;
        }

        return losers;
    }

    /// <summary>Removes a Pokémon and records when, so the removal reaches the other devices.</summary>
    private bool Forget(string id)
    {
        if (!_all.Remove(id))
        {
            return false;
        }

        _deleted[id] = _time.GetUtcNow();
        return true;
    }

    /// <summary>Stamps the changed Pokémon, saves everything right away and notifies listeners.</summary>
    private void Commit(params string[] changed)
    {
        var now = _time.GetUtcNow();
        foreach (var id in changed)
        {
            if (_all.TryGetValue(id, out var pokemon))
            {
                pokemon.UpdatedAt = now;
            }
        }

        Save();
        NotifyChanged();
        LocalDataChanged?.Invoke();
    }

    private void Save()
    {
        Status = SaveStatus.Saving;
        Status = repository.SaveAll(_all, _deleted) == SaveResult.Saved ? SaveStatus.Saved : SaveStatus.Failed;
    }

    private void UpdatePreferences(UiPreferences preferences)
    {
        Preferences = preferences;
        repository.SavePreferences(preferences);
    }

    private void NotifyChanged() => Changed?.Invoke();
}