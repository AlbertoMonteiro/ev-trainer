using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

/// <summary>Spec 016, US4 (research R5): one tap sets the lead, the Macho Brace and the Exp. Share of a plan phase.</summary>
public class TrainerStoreConfigurationTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    private readonly InMemoryRepository _repository = new(("buzz", Pokemons.Squirtle()), ("jolt", Pokemons.Pidgey()), ("other", Pokemons.Pidgey()));
    private readonly TrainerStore _store;

    public TrainerStoreConfigurationTests()
    {
        _repository.Stored["jolt"].Order = 2;
        _repository.Stored["other"].Order = 3;
        _store = new TrainerStore(_repository, Data);
        _store.Load();
        _store.Select("buzz");
    }

    [Fact]
    public void The_partner_leads_and_the_selected_one_shares()
    {
        _store.ToggleMacho();
        _store.SetExpShare("other");

        _store.ApplyConfiguration("jolt", "buzz", partnerShares: true);

        Assert.Equal("jolt", _store.SelectedId);
        Assert.True(_store.All["jolt"].MachoBrace);
        Assert.False(_store.All["buzz"].MachoBrace);
        Assert.Equal("buzz", _store.ExpShareHolderId);
        Assert.Equal(_store.All["jolt"].MachoBrace, _repository.Stored["jolt"].MachoBrace);
    }

    [Fact]
    public void Training_alone_takes_the_exp_share_away()
    {
        _store.SetExpShare("jolt");
        _repository.Stored["jolt"].MachoBrace = true;
        _store.All["jolt"].MachoBrace = true;

        _store.ApplyConfiguration("buzz", "jolt", partnerShares: false);

        Assert.True(_store.All["buzz"].MachoBrace);
        Assert.False(_store.All["jolt"].MachoBrace);
        Assert.Null(_store.ExpShareHolderId);
    }

    [Fact]
    public void Without_a_partner_only_the_lead_changes()
    {
        _store.ApplyConfiguration("jolt", null, partnerShares: false);

        Assert.Equal("jolt", _store.SelectedId);
        Assert.True(_store.All["jolt"].MachoBrace);
    }

    [Fact]
    public void Undo_restores_everything()
    {
        _store.SetExpShare("other");
        _store.All["jolt"].MachoBrace = true;

        var undo = _store.ApplyConfiguration("buzz", "jolt", partnerShares: true)!;
        undo();

        Assert.Equal("buzz", _store.SelectedId);
        Assert.False(_store.All["buzz"].MachoBrace);
        Assert.True(_store.All["jolt"].MachoBrace);
        Assert.Equal("other", _store.ExpShareHolderId);
    }

    [Fact]
    public void An_unknown_lead_does_nothing() => Assert.Null(_store.ApplyConfiguration("nobody", "buzz", partnerShares: true));
}