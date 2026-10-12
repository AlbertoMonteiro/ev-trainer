using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

/// <summary>Spec 012: a single Exp. Share holder that also gets the battles of the selected Pokémon.</summary>
public class TrainerStoreExpShareTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    private readonly InMemoryRepository _repository = new(("main", Pokemons.Squirtle()), ("abra", Pokemons.Pidgey()), ("third", Pokemons.Pidgey()));
    private readonly TrainerStore _store;

    public TrainerStoreExpShareTests()
    {
        _repository.Stored["abra"].Order = 2;
        _repository.Stored["third"].Order = 3;
        _store = new TrainerStore(_repository, Data);
        _store.Load();
        _store.Select("main");
    }

    private int[] Totals(string id) => Evs.Totals(_store.All[id].Events, Data);

    [Fact]
    public void Only_one_pokemon_holds_the_exp_share()
    {
        _store.SetExpShare("abra");
        Assert.Equal("abra", _store.ExpShareHolderId);
        Assert.True(_repository.Stored["abra"].ExpShare);

        _store.SetExpShare("third");
        Assert.Equal("third", _store.ExpShareHolderId);
        Assert.False(_repository.Stored["abra"].ExpShare);

        _store.SetExpShare(null);
        Assert.Null(_store.ExpShareHolderId);
        Assert.DoesNotContain(_repository.Stored.Values, p => p.ExpShare);
    }

    [Theory]
    [InlineData(false, false, false, 1, 1)]
    [InlineData(false, true, false, 2, 1)]
    [InlineData(false, false, true, 1, 2)]
    [InlineData(true, true, true, 4, 2)]
    public void A_battle_also_counts_for_the_holder_with_gen_3_rules(bool mainPokerus, bool mainMacho, bool holderPokerus, int mainGain, int holderGain)
    {
        _store.All["main"].Pokerus = mainPokerus;
        _store.All["main"].MachoBrace = mainMacho;
        _store.All["abra"].Pokerus = holderPokerus;
        _store.All["abra"].MachoBrace = true;
        _store.SetExpShare("abra");

        var record = _store.RecordBattle("Caterpie")!;

        Assert.Equal(("main", "abra"), (record.MainId, record.HolderId));
        Assert.Equal(mainGain, record.Main.Gain[0]);
        Assert.Equal(holderGain, record.Holder!.Gain[0]);
        Assert.Equal([mainGain, 0, 0, 0, 0, 0], Totals("main"));
        Assert.Equal([holderGain, 0, 0, 0, 0, 0], Totals("abra"));
        Assert.True(_repository.Stored["abra"].Events.Single().Shared);
    }

    [Fact]
    public void Vitamins_and_the_holder_itself_are_not_shared()
    {
        _store.SetExpShare("abra");

        var vitamin = _store.RecordBattle("HP Up")!;
        Assert.Null(vitamin.HolderId);
        Assert.Empty(_store.All["abra"].Events);

        _store.Select("abra");
        var own = _store.RecordBattle("Caterpie")!;
        Assert.Null(own.HolderId);
        Assert.Single(_store.All["abra"].Events);
        Assert.False(_store.All["abra"].Events[0].Shared);
    }

    [Fact]
    public void Without_holder_nothing_is_shared()
    {
        var record = _store.RecordBattle("Caterpie")!;

        Assert.Null(record.HolderId);
        Assert.Null(record.Holder);
        Assert.Empty(_store.All["abra"].Events);
    }

    [Fact]
    public void Deleting_the_holder_removes_the_exp_share_and_undo_brings_it_back()
    {
        _store.SetExpShare("abra");

        var undo = _store.DeletePokemon("abra")!;
        Assert.Null(_store.ExpShareHolderId);
        Assert.Null(_store.RecordBattle("Caterpie")!.HolderId);

        undo();
        Assert.Equal("abra", _store.ExpShareHolderId);
    }

    [Fact]
    public void Loading_two_holders_keeps_the_most_recently_changed()
    {
        var older = DateTimeOffset.Parse("2026-10-01T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var repository = new InMemoryRepository(("a", Pokemons.Squirtle()), ("b", Pokemons.Pidgey()));
        repository.Stored["a"].ExpShare = true;
        repository.Stored["a"].UpdatedAt = older;
        repository.Stored["b"].ExpShare = true;
        repository.Stored["b"].UpdatedAt = older.AddHours(1);
        var store = new TrainerStore(repository, Data);

        store.Load();

        Assert.Equal("b", store.ExpShareHolderId);
        Assert.False(store.All["a"].ExpShare);
    }

    [Fact]
    public void Sync_and_import_with_two_holders_keep_one()
    {
        _store.SetExpShare("abra");
        var docs = _store.Snapshot().Documents.ToDictionary(kv => kv.Key, kv => kv.Value);
        docs["third"].ExpShare = true;
        docs["third"].UpdatedAt = DateTimeOffset.MaxValue.AddDays(-1);

        _store.ApplySync(new MergeResult(docs, [], 0, 0, 0, false, true));
        Assert.Equal("third", _store.ExpShareHolderId);
        Assert.Single(_store.All.Values, p => p.ExpShare);

        var incoming = _store.Snapshot().Documents;
        incoming["main"].ExpShare = true;
        _store.ImportBackup(new BackupDocuments(new Dictionary<string, TrainingPokemon> { ["main"] = incoming["main"] }));
        Assert.Equal("main", _store.ExpShareHolderId);
        Assert.Single(_store.All.Values, p => p.ExpShare);
    }

    [Fact]
    public void Undo_reverts_both_even_after_the_exp_share_moved()
    {
        _store.SetExpShare("abra");
        _store.RecordBattle("Caterpie");
        var record = _store.RecordBattle("Caterpie")!;
        _store.SetExpShare("third");

        _store.UndoBattle(record);

        Assert.Equal([1, 0, 0, 0, 0, 0], Totals("main"));
        Assert.Equal([1, 0, 0, 0, 0, 0], Totals("abra"));
        Assert.Equal([1, 0, 0, 0, 0, 0], Evs.Totals(_repository.Stored["abra"].Events, Data));
    }
}