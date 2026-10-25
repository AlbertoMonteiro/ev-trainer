using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

/// <summary>Spec 016, research R4: a whole trainer battle recorded at once, undone at once.</summary>
public class TrainerStoreTeamTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly string[] Bernie = ["Magneton", "Magneton", "Magneton"];

    private readonly InMemoryRepository _repository = new(("buzz", Pokemons.Squirtle()), ("jolt", Pokemons.Pidgey()));
    private readonly TrainerStore _store;

    public TrainerStoreTeamTests()
    {
        _repository.Stored["jolt"].Order = 2;
        _store = new TrainerStore(_repository, Data);
        _store.Load();
        _store.Select("buzz");
    }

    private int SpAtk(string id) => Evs.Totals(_store.All[id].Events, Data)[3];

    [Fact]
    public void A_team_is_recorded_as_one_grouped_event()
    {
        var records = _store.RecordTeam(Bernie);

        Assert.Equal(3, records.Count);
        Assert.Equal(6, SpAtk("buzz"));
        var battle = Assert.Single(_store.All["buzz"].Events);
        Assert.Equal(("Magneton", 3), (battle.Opponent, battle.Count));
    }

    [Fact]
    public void The_exp_share_holder_gets_the_whole_team_too()
    {
        _store.SetExpShare("jolt");

        _store.RecordTeam(Bernie);

        Assert.Equal(6, SpAtk("buzz"));
        Assert.Equal(6, SpAtk("jolt"));
        Assert.True(Assert.Single(_store.All["jolt"].Events).Shared);
    }

    [Fact]
    public void Macho_brace_doubles_only_the_lead()
    {
        _store.ToggleMacho();
        _store.SetExpShare("jolt");

        _store.RecordTeam(Bernie);

        Assert.Equal(12, SpAtk("buzz"));
        Assert.Equal(6, SpAtk("jolt"));
    }

    [Fact]
    public void Undo_restores_both_pokemon()
    {
        _store.RecordBattle("Caterpie");
        _store.SetExpShare("jolt");
        var before = (_store.All["buzz"].Events.Count, _store.All["jolt"].Events.Count);

        _store.UndoTeam(_store.RecordTeam(["Magnemite", "Magnemite", "Magneton"]));

        Assert.Equal(before, (_store.All["buzz"].Events.Count, _store.All["jolt"].Events.Count));
        Assert.Equal(0, SpAtk("buzz"));
        Assert.Equal(0, SpAtk("jolt"));
    }

    [Fact]
    public void Nothing_selected_records_nothing()
    {
        var store = new TrainerStore(new InMemoryRepository(), Data);
        store.Load();

        Assert.Empty(store.RecordTeam(Bernie));
    }
}