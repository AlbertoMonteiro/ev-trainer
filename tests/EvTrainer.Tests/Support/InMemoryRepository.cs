using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.Support;

public sealed class InMemoryRepository : IPokemonRepository
{
    public Dictionary<string, TrainingPokemon> Stored { get; private set; } = [];
    public UiPreferences Preferences { get; private set; } = new();
    public int SaveCount { get; private set; }
    public bool FailSaves { get; set; }

    public InMemoryRepository(params (string Id, TrainingPokemon Pokemon)[] seed)
    {
        foreach (var (id, pokemon) in seed)
        {
            Stored[id] = pokemon.Clone();
        }
    }

    public IReadOnlyDictionary<string, TrainingPokemon> LoadAll() =>
        Stored.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());

    public SaveResult SaveAll(IReadOnlyDictionary<string, TrainingPokemon> all)
    {
        SaveCount++;
        if (FailSaves)
        {
            return SaveResult.Failed;
        }

        Stored = all.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
        return SaveResult.Saved;
    }

    public UiPreferences LoadPreferences() => Preferences with { };

    public void SavePreferences(UiPreferences preferences) => Preferences = preferences with { };
}