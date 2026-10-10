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

    [Fact]
    public void AdjustEvs_appends_an_adjustment_and_undo_removes_it()
    {
        var repository = new InMemoryRepository(("a", Pokemons.Squirtle().WithEvents(("Caterpie", 10, 1))));
        var store = CreateStore(repository);

        var undo = store.AdjustEvs([12, 0, 2, 26, 0, 4]);

        Assert.NotNull(undo);
        Assert.Equal([12, 0, 2, 26, 0, 4], store.SelectedTotals);
        Assert.Equal(2, repository.Stored["a"].Events.Count);
        Assert.True(repository.Stored["a"].Events[^1].IsManualAdjustment);

        undo();

        Assert.Equal([10, 0, 0, 0, 0, 0], store.SelectedTotals);
        Assert.Single(repository.Stored["a"].Events);
    }

    [Fact]
    public void AdjustEvs_with_the_current_totals_does_nothing()
    {
        var repository = new InMemoryRepository(("a", Pokemons.Squirtle().WithEvents(("Caterpie", 10, 1))));
        var store = CreateStore(repository);
        var saves = repository.SaveCount;

        Assert.Null(store.AdjustEvs([10, 0, 0, 0, 0, 0]));
        Assert.Equal(saves, repository.SaveCount);
    }

    [Theory]
    [InlineData(new[] { 256, 0, 0, 0, 0, 0 })]
    [InlineData(new[] { -1, 0, 0, 0, 0, 0 })]
    [InlineData(new[] { 255, 255, 1, 0, 0, 0 })]
    public void Invalid_targets_are_rejected(int[] targets)
    {
        var repository = new InMemoryRepository(("a", Pokemons.Squirtle()));
        var store = CreateStore(repository);

        Assert.Null(store.AdjustEvs(targets));
        Assert.Null(store.ResetHistory(targets));
        Assert.Equal(0, repository.SaveCount);
    }

    private static LevelEntry Note(int level, int hp) =>
        new() { Level = level, Species = "Wartortle", Stats = [hp, 1, 1, 1, 1, 1], Evs = new int[6] };

    [Fact]
    public void ReplaceLevel_replaces_in_place_and_undo_restores()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.IvLog.AddRange([Note(16, 49), Note(21, 62), Note(25, 70)]);
        var repository = new InMemoryRepository(("a", pokemon));
        var store = CreateStore(repository);

        var undo = store.ReplaceLevel(1, Note(21, 63));

        Assert.Equal([49, 63, 70], repository.Stored["a"].IvLog.Select(l => l.Stats[0]));

        undo!();

        Assert.Equal([49, 62, 70], repository.Stored["a"].IvLog.Select(l => l.Stats[0]));
    }

    [Fact]
    public void ReplaceLevel_reorders_when_the_level_changes_and_undo_puts_it_back()
    {
        var pokemon = Pokemons.Squirtle();
        pokemon.IvLog.AddRange([Note(16, 49), Note(21, 62), Note(25, 70)]);
        var repository = new InMemoryRepository(("a", pokemon));
        var store = CreateStore(repository);

        var undo = store.ReplaceLevel(0, Note(30, 80));

        Assert.Equal([21, 25, 30], repository.Stored["a"].IvLog.Select(l => l.Level));

        undo!();

        Assert.Equal([16, 21, 25], repository.Stored["a"].IvLog.Select(l => l.Level));
        Assert.Equal([49, 62, 70], repository.Stored["a"].IvLog.Select(l => l.Stats[0]));
    }

    [Fact]
    public void ReplaceLevel_with_an_invalid_index_does_nothing()
    {
        var repository = new InMemoryRepository(("a", Pokemons.Squirtle()));
        var store = CreateStore(repository);

        Assert.Null(store.ReplaceLevel(0, Note(5, 20)));
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public void SetKnownIvs_validates_and_undo_restores()
    {
        var repository = new InMemoryRepository(("a", Pokemons.Squirtle()));
        var store = CreateStore(repository);

        Assert.Null(store.SetKnownIvs([32, null, null, null, null, null]));
        Assert.Null(store.SetKnownIvs([1, 2]));

        var undo = store.SetKnownIvs([null, null, null, null, null, 30]);

        Assert.Equal([null, null, null, null, null, 30], repository.Stored["a"].KnownIvs);
        undo!();
        Assert.Equal(new int?[6], repository.Stored["a"].KnownIvs);
    }

    [Fact]
    public void ResetHistory_keeps_only_an_adjustment_and_undo_restores_everything()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 10, 1), ("Abra", 2, 2));
        pokemon.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 11], Evs = new int[6] });
        var repository = new InMemoryRepository(("a", pokemon));
        var store = CreateStore(repository);

        var undo = store.ResetHistory([12, 0, 2, 26, 0, 4]);

        var only = Assert.Single(repository.Stored["a"].Events);
        Assert.Equal([12, 0, 2, 26, 0, 4], only.SetTotals!);
        Assert.Single(repository.Stored["a"].IvLog);

        undo!();

        Assert.Equal(
            [("Caterpie", 10, 1), ("Abra", 2, 2)],
            repository.Stored["a"].Events.Select(e => (e.Opponent, e.Count, e.Multiplier)));
    }
}