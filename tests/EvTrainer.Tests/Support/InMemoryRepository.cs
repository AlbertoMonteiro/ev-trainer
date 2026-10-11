using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.Support;

public sealed class InMemoryRepository : IPokemonRepository
{
    public Dictionary<string, TrainingPokemon> Stored { get; private set; } = [];
    public UiPreferences Preferences { get; private set; } = new();
    public int SaveCount { get; private set; }
    public bool FailSaves { get; set; }
    public CaptureNotes CaptureNotes { get; private set; } = new();
    public int CaptureSaveCount { get; private set; }
    public Dictionary<string, DateTimeOffset> Deleted { get; private set; } = [];
    public SyncConnection? SyncConnection { get; private set; }

    public InMemoryRepository(params (string Id, TrainingPokemon Pokemon)[] seed)
    {
        foreach (var (id, pokemon) in seed)
        {
            Stored[id] = pokemon.Clone();
        }
    }

    public IReadOnlyDictionary<string, TrainingPokemon> LoadAll() =>
        Stored.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());

    public IReadOnlyDictionary<string, DateTimeOffset> LoadDeleted() => new Dictionary<string, DateTimeOffset>(Deleted);

    public SaveResult SaveAll(IReadOnlyDictionary<string, TrainingPokemon> all, IReadOnlyDictionary<string, DateTimeOffset> deleted)
    {
        SaveCount++;
        if (FailSaves)
        {
            return SaveResult.Failed;
        }

        Stored = all.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
        Deleted = new Dictionary<string, DateTimeOffset>(deleted);
        return SaveResult.Saved;
    }

    public UiPreferences LoadPreferences() => Preferences with { };

    public void SavePreferences(UiPreferences preferences) => Preferences = preferences with { };

    public CaptureNotes LoadCaptureNotes() => CaptureNotes.Clone();

    public SyncConnection? LoadSyncConnection() => SyncConnection;

    public void SaveSyncConnection(SyncConnection connection) => SyncConnection = connection;

    public void ClearSyncConnection() => SyncConnection = null;

    public void SaveCaptureNotes(CaptureNotes notes)
    {
        CaptureSaveCount++;
        CaptureNotes = notes.Clone();
    }
}