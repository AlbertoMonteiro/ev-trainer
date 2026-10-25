using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

/// <summary>Spec 016: the current party (at most six) and the stored Pokémon.</summary>
public class TrainerStorePartyTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly DateTimeOffset Base = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ManualTimeProvider _time = new(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Pokémon "p1".."pN" (Order = i, stamped i hours after Base) with the given party marks.</summary>
    private static InMemoryRepository Repository(int count, Func<int, bool?> party)
    {
        var repository = new InMemoryRepository();
        for (var i = 1; i <= count; i++)
        {
            var pokemon = Pokemons.Pidgey();
            pokemon.Order = i;
            pokemon.Party = party(i);
            pokemon.UpdatedAt = Base.AddHours(i);
            repository.Stored[$"p{i}"] = pokemon;
        }

        return repository;
    }

    private TrainerStore Loaded(InMemoryRepository repository)
    {
        var store = new TrainerStore(repository, Data, _time);
        store.Load();
        return store;
    }

    private static void AssertConsistent(IReadOnlyDictionary<string, TrainingPokemon> all)
    {
        Assert.DoesNotContain(all.Values, p => p.Party is null);
        Assert.True(all.Values.Count(p => p.Party == true) <= Party.MaxSize);
        Assert.DoesNotContain(all.Values, p => p.ExpShare && p.Party != true);
    }

    [Fact]
    public void Load_fills_the_party_from_old_data_and_saves_without_stamping()
    {
        var repository = Repository(8, _ => null);

        var store = Loaded(repository);

        AssertConsistent(repository.Stored);
        Assert.Equal(["p3", "p4", "p5", "p6", "p7", "p8"], store.PartyOrdered.Select(p => p.Id));
        Assert.Equal(["p1", "p2"], store.Stored.Select(p => p.Id));
        Assert.Equal(Base.AddHours(1), repository.Stored["p1"].UpdatedAt);
        Assert.True(store.IsInParty("p3"));
        Assert.False(store.IsInParty("p1"));
    }

    [Fact]
    public void Import_keeps_the_party_consistent()
    {
        var store = Loaded(Repository(5, _ => true));
        var incoming = new Dictionary<string, TrainingPokemon>();
        for (var i = 1; i <= 3; i++)
        {
            var pokemon = Pokemons.Squirtle();
            pokemon.Order = 10 + i;
            incoming[$"new{i}"] = pokemon;
        }

        store.ImportBackup(new BackupDocuments(incoming));

        AssertConsistent(store.All);
        Assert.Equal(Party.MaxSize, store.PartyOrdered.Count);
    }

    [Fact]
    public void Sync_with_seven_in_the_party_stores_the_oldest()
    {
        var store = Loaded(Repository(1, _ => true));
        var documents = new Dictionary<string, TrainingPokemon>();
        for (var i = 1; i <= 7; i++)
        {
            var pokemon = Pokemons.Pidgey();
            pokemon.Order = i;
            pokemon.Party = true;
            pokemon.UpdatedAt = Base.AddHours(i == 5 ? 0 : 10 + i);
            documents[$"s{i}"] = pokemon;
        }

        documents["s5"].ExpShare = true;

        store.ApplySync(new MergeResult(documents, [], Updated: 0, Added: 7, Removed: 0, LocalChanged: true, RemoteChanged: false));

        AssertConsistent(store.All);
        Assert.False(store.IsInParty("s5"));
        Assert.Null(store.ExpShareHolderId);
    }

    [Fact]
    public void Join_with_a_free_place_enters_the_party()
    {
        var store = Loaded(Repository(6, i => i != 3));

        Assert.True(store.JoinParty("p3"));

        Assert.True(store.IsInParty("p3"));
        Assert.Equal(_time.GetUtcNow(), store.All["p3"].UpdatedAt);
    }

    [Fact]
    public void Join_with_a_full_party_needs_someone_to_leave()
    {
        var store = Loaded(Repository(7, i => i != 7));

        Assert.False(store.JoinParty("p7"));
        Assert.False(store.IsInParty("p7"));

        Assert.True(store.JoinParty("p7", replacing: "p2"));
        Assert.True(store.IsInParty("p7"));
        Assert.False(store.IsInParty("p2"));
        Assert.Equal(Party.MaxSize, store.PartyOrdered.Count);
    }

    [Fact]
    public void Leaving_keeps_every_training_data_and_drops_the_exp_share()
    {
        var repository = Repository(2, _ => true);
        repository.Stored["p1"] = Pokemons.Squirtle().WithEvents(("Caterpie", 4, 2));
        repository.Stored["p1"].Party = true;
        repository.Stored["p1"].Pokerus = true;
        repository.Stored["p1"].MachoBrace = true;
        repository.Stored["p1"].KnownIvs = [31, null, null, null, null, null];
        var store = Loaded(repository);
        store.SetExpShare("p1");
        var before = store.All["p1"].Clone();

        store.LeaveParty("p1");

        var after = store.All["p1"];
        Assert.False(after.Party);
        Assert.False(after.ExpShare);
        Assert.Equal(before.Events.Select(e => (e.Opponent, e.Count, e.Multiplier)), after.Events.Select(e => (e.Opponent, e.Count, e.Multiplier)));
        Assert.Equal(before.Targets, after.Targets);
        Assert.Equal(before.KnownIvs, after.KnownIvs);
        Assert.Equal((true, true), (after.Pokerus, after.MachoBrace));
    }

    [Fact]
    public void Moving_swaps_the_order_with_the_party_neighbour()
    {
        var store = Loaded(Repository(4, i => i != 2));

        store.MoveInParty("p3", -1);

        Assert.Equal(["p3", "p1", "p4"], store.PartyOrdered.Select(p => p.Id));
        Assert.Equal(_time.GetUtcNow(), store.All["p3"].UpdatedAt);
        Assert.Equal(_time.GetUtcNow(), store.All["p1"].UpdatedAt);

        store.MoveInParty("p3", -1);
        store.MoveInParty("p4", 1);
        Assert.Equal(["p3", "p1", "p4"], store.PartyOrdered.Select(p => p.Id));
    }

    [Fact]
    public void A_new_pokemon_joins_when_there_is_room()
    {
        var store = Loaded(Repository(5, _ => true));

        var id = store.AddPokemon();

        Assert.True(store.IsInParty(id));
    }

    [Fact]
    public void A_new_pokemon_is_stored_and_selected_when_the_party_is_full()
    {
        var store = Loaded(Repository(6, _ => true));

        var id = store.AddPokemon();

        Assert.False(store.IsInParty(id));
        Assert.Equal(id, store.SelectedId);
        Assert.False(store.All[id].Party is null);
    }

    [Fact]
    public void A_captured_pokemon_follows_the_same_rule()
    {
        var store = Loaded(Repository(6, _ => true));
        var dex = Pokedex.LoadEmbedded();

        var (id, _) = store.AddFromCapture(new CaptureInput("Abra", 10, "Timid", [25, 10, 10, 30, 20, 25]), dex);

        Assert.False(store.IsInParty(id));
    }

    [Fact]
    public void Undoing_a_deletion_returns_to_the_party_only_with_room()
    {
        var store = Loaded(Repository(6, _ => true));
        var undo = store.DeletePokemon("p2")!;
        store.AddPokemon();

        undo();

        Assert.False(store.IsInParty("p2"));
        Assert.True(store.All.ContainsKey("p2"));

        var other = Loaded(Repository(6, _ => true));
        var undoOther = other.DeletePokemon("p2")!;
        undoOther();
        Assert.True(other.IsInParty("p2"));
    }

    [Fact]
    public void The_exp_share_only_goes_to_the_party()
    {
        var store = Loaded(Repository(3, i => i != 3));

        store.SetExpShare("p3");
        Assert.Null(store.ExpShareHolderId);

        store.SetExpShare("p2");
        Assert.Equal("p2", store.ExpShareHolderId);
    }

    [Fact]
    public void The_backup_carries_the_party_of_every_pokemon()
    {
        var store = Loaded(Repository(8, _ => null));

        var documents = System.Text.Json.Nodes.JsonNode.Parse(store.ExportBackup())!["documents"]!.AsObject();

        Assert.Equal(6, documents.Count(d => d.Value!["party"]!.GetValue<bool>()));
        Assert.Equal(2, documents.Count(d => !d.Value!["party"]!.GetValue<bool>()));
        var restored = Loaded(new InMemoryRepository());
        restored.ImportBackup(new BackupDocuments(PokemonJson.Deserialize(store.ExportBackup())!));
        Assert.Equal(store.PartyOrdered.Select(p => p.Id), restored.PartyOrdered.Select(p => p.Id));
    }
}