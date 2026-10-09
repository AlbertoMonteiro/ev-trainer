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
/// <see cref="IPokemonRepository"/> and raises <see cref="Changed"/>. Holds no UI text.
/// </summary>
public sealed partial class TrainerStore(IPokemonRepository repository, ReferenceData data)
{
    private readonly Dictionary<string, TrainingPokemon> _all = new(StringComparer.Ordinal);

    public event Action? Changed;

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
        Commit();
        return id;
    }

    /// <summary>Records a battle for the selected Pokémon; null when nothing is selected.</summary>
    public RecordResult? RecordBattle(string opponent)
    {
        if (Selected is not { } pokemon)
        {
            return null;
        }

        var result = BattleLog.Record(pokemon, opponent, data);
        Commit();
        return result;
    }

    public void UndoLastBattle(string id)
    {
        if (_all.TryGetValue(id, out var pokemon))
        {
            BattleLog.UndoLast(pokemon);
            Commit();
        }
    }

    public void TogglePokerus()
    {
        if (Selected is { } pokemon)
        {
            pokemon.Pokerus = !pokemon.Pokerus;
            Commit();
        }
    }

    public void ToggleMacho()
    {
        if (Selected is { } pokemon)
        {
            pokemon.MachoBrace = !pokemon.MachoBrace;
            Commit();
        }
    }

    public void SaveSettings(string name, string nature, int[] targets, IReadOnlyList<SpeciesEntry> species)
    {
        if (Selected is not { } pokemon)
        {
            return;
        }

        pokemon.Name = name.Trim();
        pokemon.Nature = nature;
        pokemon.Targets = targets.ToSix();
        pokemon.Species = [.. species.Select(s => s.Clone())];
        Commit();
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
        Commit();
        return () =>
        {
            if (_all.TryGetValue(id, out var pokemon))
            {
                pokemon.Events.Insert(Math.Min(index, pokemon.Events.Count), removed);
                Commit();
            }
        };
    }

    /// <summary>Deletes a Pokémon (selecting the first remaining one); the returned undo restores it.</summary>
    public Action? DeletePokemon(string id)
    {
        if (!_all.Remove(id, out var removed))
        {
            return null;
        }

        if (Preferences.SelectedId == id)
        {
            UpdatePreferences(Preferences with { SelectedId = Ordered.FirstOrDefault().Id });
        }

        Commit();
        return () =>
        {
            _all[id] = removed;
            UpdatePreferences(Preferences with { SelectedId = id });
            Commit();
        };
    }

    /// <summary>Appends imported battles to the end of the log.</summary>
    public void ImportBattles(IReadOnlyList<BattleEvent> events)
    {
        if (Selected is { } pokemon)
        {
            pokemon.Events.AddRange(events.Select(e => e.Clone()));
            Commit();
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

            Commit();
        }
    }

    /// <summary>Adds a level note keeping the log sorted by level.</summary>
    public void AddLevel(LevelEntry entry)
    {
        if (Selected is { } pokemon)
        {
            pokemon.InsertLevel(entry.Clone());
            Commit();
        }
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
        Commit();
        return () =>
        {
            if (_all.TryGetValue(id, out var pokemon))
            {
                pokemon.IvLog.Insert(Math.Min(index, pokemon.IvLog.Count), removed);
                Commit();
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
        }

        if (SelectedId is null && Ordered.Count > 0)
        {
            UpdatePreferences(Preferences with { SelectedId = backup.Items.Keys.FirstOrDefault() ?? Ordered[0].Id });
        }

        Commit();
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

    public void SetLanguage(string language)
    {
        UpdatePreferences(Preferences with { Language = language });
        NotifyChanged();
    }

    /// <summary>Saves everything right away and notifies listeners.</summary>
    private void Commit()
    {
        Status = SaveStatus.Saving;
        Status = repository.SaveAll(_all) == SaveResult.Saved ? SaveStatus.Saved : SaveStatus.Failed;
        NotifyChanged();
    }

    private void UpdatePreferences(UiPreferences preferences)
    {
        Preferences = preferences;
        repository.SavePreferences(preferences);
    }

    private void NotifyChanged() => Changed?.Invoke();
}