using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

/// <summary>"Changed at" stamps and deletion records that feed the sync (spec 008, research R5).</summary>
public class TrainerStoreStampTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    private readonly ManualTimeProvider _time = new();
    private readonly InMemoryRepository _repository;
    private readonly TrainerStore _store;

    public TrainerStoreStampTests()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 3, 1));
        pokemon.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 11], Evs = new int[6] });
        _repository = new InMemoryRepository(("a", pokemon), ("b", Pokemons.Squirtle()));
        _repository.Stored["b"].Order = 2;
        _store = new TrainerStore(_repository, Data, _time);
        _store.Load();
        _store.Select("a");
        _time.Advance(TimeSpan.FromMinutes(1));
    }

    private DateTimeOffset Now => _time.GetUtcNow();

    private DateTimeOffset? StoredStamp(string id) => _repository.Stored[id].UpdatedAt;

    public static TheoryData<string> Commands =>
    [
        "RecordBattle", "UndoLastBattle", "TogglePokerus", "ToggleMacho", "SaveSettings", "RemoveEvent", "RemoveEventUndo",
        "AdjustEvs", "ResetHistory", "SetKnownIvs", "AddLevel", "RemoveLevel", "ReplaceLevel", "ImportBattles", "ImportLevels",
    ];

    [Theory]
    [MemberData(nameof(Commands))]
    public void Every_command_that_changes_the_selected_pokemon_stamps_it(string command)
    {
        switch (command)
        {
            case "RecordBattle": _store.RecordBattle("Caterpie"); break;
            case "UndoLastBattle": _store.UndoLastBattle("a"); break;
            case "TogglePokerus": _store.TogglePokerus(); break;
            case "ToggleMacho": _store.ToggleMacho(); break;
            case "SaveSettings": _store.SaveSettings("Squirtle", "Bold", [0, 0, 0, 0, 0, 0], [new SpeciesEntry { Name = "Squirtle", Base = [44, 48, 65, 50, 64, 43] }]); break;
            case "RemoveEvent": _store.RemoveEvent(0); break;
            case "RemoveEventUndo":
                var undo = _store.RemoveEvent(0)!;
                _time.Advance(TimeSpan.FromMinutes(1));
                undo();
                break;
            case "AdjustEvs": _store.AdjustEvs([1, 0, 0, 0, 0, 0]); break;
            case "ResetHistory": _store.ResetHistory([1, 0, 0, 0, 0, 0]); break;
            case "SetKnownIvs": _store.SetKnownIvs([null, null, null, null, null, 30]); break;
            case "AddLevel": _store.AddLevel(new LevelEntry { Level = 9, Species = "Squirtle", Stats = [25, 12, 16, 14, 16, 14], Evs = new int[6] }); break;
            case "RemoveLevel": _store.RemoveLevel(0); break;
            case "ReplaceLevel": _store.ReplaceLevel(0, new LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 12], Evs = new int[6] }); break;
            case "ImportBattles": _store.ImportBattles([new BattleEvent { Opponent = "Pidgey", Count = 1, Multiplier = 1 }]); break;
            case "ImportLevels": _store.ImportLevels([new LevelEntry { Level = 7, Species = "Squirtle", Stats = [23, 11, 14, 13, 14, 13], Evs = new int[6] }]); break;
        }

        Assert.Equal(Now, StoredStamp("a"));
        Assert.Null(StoredStamp("b"));
    }

    [Fact]
    public void Preferences_do_not_stamp()
    {
        _store.Select("b");
        _store.SetFilter("all");
        _store.SetTheme("dark");
        _store.SetLanguage("pt-BR");

        Assert.Null(StoredStamp("a"));
        Assert.Null(StoredStamp("b"));
    }

    [Fact]
    public void New_pokemon_are_stamped()
    {
        var added = _store.AddPokemon();
        var (captured, _) = _store.AddFromCapture(new CaptureInput("Squirtle", 5, "Timid", [20, 9, 12, 11, 12, 11]), Dex);

        Assert.Equal(Now, StoredStamp(added));
        Assert.Equal(Now, StoredStamp(captured));
    }

    [Fact]
    public void Imported_backup_pokemon_are_stamped()
    {
        var imported = Pokemons.Squirtle();
        imported.UpdatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        _store.ImportBackup(new BackupDocuments(new Dictionary<string, TrainingPokemon> { ["z"] = imported }));

        Assert.Equal(Now, StoredStamp("z"));
        Assert.Null(StoredStamp("b"));
    }

    [Fact]
    public void Deleting_records_the_deletion_and_undo_removes_it_and_stamps()
    {
        var undo = _store.DeletePokemon("b")!;

        Assert.Equal(Now, _repository.Deleted["b"]);
        Assert.Equal(Now, _store.Snapshot().Deleted["b"]);

        _time.Advance(TimeSpan.FromMinutes(1));
        undo();

        Assert.False(_repository.Deleted.ContainsKey("b"));
        Assert.Equal(Now, StoredStamp("b"));
    }

    [Fact]
    public void Undoing_an_added_capture_records_a_deletion()
    {
        var (id, undo) = _store.AddFromCapture(new CaptureInput("Squirtle", 5, "Timid", [20, 9, 12, 11, 12, 11]), Dex);

        undo();

        Assert.True(_repository.Deleted.ContainsKey(id));
    }

    [Fact]
    public void Deletions_survive_a_reload()
    {
        _store.DeletePokemon("b");

        var reloaded = new TrainerStore(_repository, Data, _time);
        reloaded.Load();

        Assert.Equal(Now, reloaded.Snapshot().Deleted["b"]);
    }

    [Fact]
    public void Snapshot_is_a_copy_of_documents_and_deletions()
    {
        var snapshot = _store.Snapshot();
        snapshot.Documents["a"].Pokerus = true;

        Assert.False(_store.All["a"].Pokerus);
        Assert.Equal(["a", "b"], snapshot.Documents.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Apply_sync_replaces_without_stamping_keeps_selection_and_saves()
    {
        var incoming = Pokemons.Squirtle();
        incoming.CurrentSpecies = "Wartortle";
        incoming.UpdatedAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var deletedAt = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var changes = 0;
        _store.Changed += () => changes++;
        var localEdits = 0;
        _store.LocalDataChanged += () => localEdits++;

        _store.ApplySync(new MergeResult(
            new Dictionary<string, TrainingPokemon> { ["a"] = incoming },
            new Dictionary<string, DateTimeOffset> { ["b"] = deletedAt },
            Updated: 1, Added: 0, Removed: 1, LocalChanged: true, RemoteChanged: false));

        Assert.Equal(["a"], _repository.Stored.Keys);
        Assert.Equal("Wartortle", _repository.Stored["a"].CurrentSpecies);
        Assert.Equal(incoming.UpdatedAt, StoredStamp("a"));
        Assert.Equal(deletedAt, _repository.Deleted["b"]);
        Assert.Equal("a", _store.SelectedId);
        Assert.Equal(1, changes);
        Assert.Equal(0, localEdits);
    }

    [Fact]
    public void Apply_sync_selects_the_first_when_the_selected_one_is_gone()
    {
        _store.ApplySync(new MergeResult(
            new Dictionary<string, TrainingPokemon> { ["b"] = Pokemons.Squirtle() },
            new Dictionary<string, DateTimeOffset> { ["a"] = Now },
            Updated: 0, Added: 0, Removed: 1, LocalChanged: true, RemoteChanged: false));

        Assert.Equal("b", _store.SelectedId);
    }

    [Fact]
    public void Local_edits_raise_local_data_changed()
    {
        var localEdits = 0;
        _store.LocalDataChanged += () => localEdits++;

        _store.RecordBattle("Caterpie");
        _store.Select("b");
        _store.DeletePokemon("b");

        Assert.Equal(2, localEdits);
    }
}