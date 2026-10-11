using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

public class CaptureStoreTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    private static CaptureStore CreateStore(InMemoryRepository repository) => new(repository, Dex, Data);

    [Fact]
    public void Counted_defaults_to_the_species_rule()
    {
        var store = CreateStore(new InMemoryRepository());

        Assert.Equal([true, false, true, true, true, true], store.CountedFor("Abra"));
        Assert.Equal([true, true, true, false, true, true], store.CountedFor("Machop"));
    }

    [Fact]
    public void SetCounted_saves_notifies_and_survives_a_reload()
    {
        var repository = new InMemoryRepository();
        var store = CreateStore(repository);
        var changes = 0;
        store.Changed += () => changes++;

        store.SetCounted("Abra", [true, true, true, true, true, true]);

        Assert.Equal(1, changes);
        Assert.Equal(1, repository.CaptureSaveCount);
        Assert.Equal([true, true, true, true, true, true], CreateStore(repository).CountedFor("Abra"));
    }

    [Fact]
    public void ResetCounted_goes_back_to_the_default()
    {
        var repository = new InMemoryRepository();
        var store = CreateStore(repository);
        store.SetCounted("Abra", [true, true, true, true, true, true]);

        store.ResetCounted("Abra");

        Assert.Equal([true, false, true, true, true, true], store.CountedFor("Abra"));
        Assert.Empty(repository.CaptureNotes.Counted);
    }

    [Fact]
    public void Returned_arrays_are_copies()
    {
        var store = CreateStore(new InMemoryRepository());

        store.CountedFor("Abra")[1] = true;

        Assert.False(store.CountedFor("Abra")[1]);
    }

    // Squirtle Nv 5 Timid with every IV 0 and with every IV 31.
    private static readonly CaptureInput Weak = new("Squirtle", 5, "Timid", [19, 8, 11, 10, 11, 9]);
    private static readonly CaptureInput Strong = new("Squirtle", 5, "Timid", [20, 9, 13, 11, 12, 11]);

    // Strong, but with ATK IV 0–3 (ATK does not count by default for Squirtle).
    private static readonly CaptureInput WeakAttack = Strong with { Stats = [20, 8, 13, 11, 12, 11] };

    [Fact]
    public void Invalid_or_inconsistent_captures_are_never_kept()
    {
        var repository = new InMemoryRepository();
        var store = CreateStore(repository);

        Assert.Null(store.Conclude(Weak with { Level = 0 }));
        Assert.Null(store.Conclude(Weak with { Stats = [99, 9, 12, 11, 12, 11] }));
        Assert.Null(store.BestFor("Squirtle"));
        Assert.Equal(0, repository.CaptureSaveCount);
    }

    [Fact]
    public void First_capture_becomes_the_best_and_undo_forgets_it()
    {
        var repository = new InMemoryRepository();
        var store = CreateStore(repository);

        var undo = store.Conclude(Weak);

        Assert.Equal(Weak.Stats, store.BestFor("Squirtle")!.Stats);
        Assert.Equal(Weak.Stats, repository.CaptureNotes.Best["Squirtle"].Stats);

        undo!();

        Assert.Null(store.BestFor("Squirtle"));
    }

    [Fact]
    public void Better_capture_replaces_the_best_and_undo_restores_it()
    {
        var store = CreateStore(new InMemoryRepository());
        store.Conclude(Weak);

        Assert.Equal(CaptureComparison.Better, store.CompareWithBest(Strong));
        var undo = store.Conclude(Strong);

        Assert.Equal(Strong.Stats, store.BestFor("Squirtle")!.Stats);

        undo!();

        Assert.Equal(Weak.Stats, store.BestFor("Squirtle")!.Stats);
    }

    [Fact]
    public void Worse_or_equivalent_capture_does_not_replace_the_best()
    {
        var repository = new InMemoryRepository();
        var store = CreateStore(repository);
        store.Conclude(Strong);
        var saves = repository.CaptureSaveCount;

        Assert.Equal(CaptureComparison.Worse, store.CompareWithBest(Weak));
        Assert.Null(store.Conclude(Weak));
        Assert.Equal(CaptureComparison.Equivalent, store.CompareWithBest(Strong));
        Assert.Null(store.Conclude(Strong));
        Assert.Equal(Strong.Stats, store.BestFor("Squirtle")!.Stats);
        Assert.Equal(saves, repository.CaptureSaveCount);
    }

    [Fact]
    public void Comparison_uses_the_current_counted_stats_for_both_sides()
    {
        var store = CreateStore(new InMemoryRepository());
        store.Conclude(Strong);

        Assert.Equal(CaptureComparison.Equivalent, store.CompareWithBest(WeakAttack));

        store.SetCounted("Squirtle", [true, true, true, true, true, true]);

        Assert.Equal(CaptureComparison.Worse, store.CompareWithBest(WeakAttack));
    }

    [Fact]
    public void Forget_removes_the_best_with_undo()
    {
        var store = CreateStore(new InMemoryRepository());
        store.Conclude(Weak);

        var undo = store.Forget("Squirtle");

        Assert.Null(store.BestFor("Squirtle"));
        undo!();
        Assert.NotNull(store.BestFor("Squirtle"));
        Assert.Null(store.Forget("Abra"));
    }

    [Fact]
    public void No_best_means_no_comparison()
    {
        var store = CreateStore(new InMemoryRepository());

        Assert.Null(store.CompareWithBest(Weak));
    }
}