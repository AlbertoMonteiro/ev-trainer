using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

public class TrainerStoreTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    private static TrainerStore CreateStore(InMemoryRepository repository)
    {
        var store = new TrainerStore(repository, Data);
        store.Load();
        return store;
    }

    [Fact]
    public void Empty_repository_gives_empty_store()
    {
        var store = CreateStore(new InMemoryRepository());

        Assert.Empty(store.Ordered);
        Assert.Null(store.Selected);
        Assert.Equal(SaveStatus.Saved, store.Status);
    }

    [Fact]
    public void AddPokemon_creates_blank_pokemon_selects_and_saves_immediately()
    {
        var repository = new InMemoryRepository(("old", Pokemons.Squirtle()));
        repository.Stored["old"].Order = 3;
        var store = CreateStore(repository);

        var id = store.AddPokemon();

        Assert.Matches("^[0-9a-f]{32}$", id);
        Assert.Equal(id, store.SelectedId);
        var created = store.Selected!;
        var species = Assert.Single(created.Species);
        Assert.Equal("", species.Name);
        Assert.Equal(new int[6], created.Targets);
        Assert.Equal(4, created.Order);
        Assert.Equal(1, repository.SaveCount);
        Assert.True(repository.Stored.ContainsKey(id));
        Assert.Equal(id, repository.Preferences.SelectedId);
    }

    [Fact]
    public void Failed_save_keeps_state_in_memory()
    {
        var repository = new InMemoryRepository { FailSaves = true };
        var store = CreateStore(repository);

        var id = store.AddPokemon();

        Assert.Equal(SaveStatus.Failed, store.Status);
        Assert.NotNull(store.Selected);
        Assert.Equal(id, store.SelectedId);
    }

    [Fact]
    public void Changed_fires_for_each_command()
    {
        var store = CreateStore(new InMemoryRepository());
        var changes = 0;
        store.Changed += () => changes++;

        store.AddPokemon();
        store.SetFilter("all");

        Assert.Equal(2, changes);
    }

    [Fact]
    public void Invalid_selected_id_falls_back_to_first_by_order()
    {
        var first = Pokemons.Squirtle();
        first.Order = 1;
        var second = Pokemons.Squirtle();
        second.Order = 2;
        var repository = new InMemoryRepository(("b", second), ("a", first));
        repository.SavePreferences(new UiPreferences { SelectedId = "missing" });

        var store = CreateStore(repository);

        Assert.Equal("a", store.SelectedId);
        Assert.Equal(["a", "b"], store.Ordered.Select(o => o.Id));
    }

    [Fact]
    public void Preferences_are_loaded_and_saved()
    {
        var repository = new InMemoryRepository(("a", Pokemons.Squirtle()));
        repository.SavePreferences(new UiPreferences { SelectedId = "a", Theme = "dark", Filter = "all" });
        var store = CreateStore(repository);

        Assert.Equal("dark", store.Preferences.Theme);
        Assert.Equal("all", store.Preferences.Filter);

        store.SetTheme("light");
        store.SetFilter("useful");

        Assert.Equal("light", repository.Preferences.Theme);
        Assert.Equal("useful", repository.Preferences.Filter);
    }
}