using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.State;

namespace EvTrainer.Tests.State;

public class CaptureStoreTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    private static CaptureStore CreateStore(InMemoryRepository repository) => new(repository, Dex, Data);

    private const StatImportance I = StatImportance.Ignored;
    private const StatImportance C = StatImportance.Counts;
    private const StatImportance M = StatImportance.Main;

    [Fact]
    public void Importance_defaults_to_the_species_rule()
    {
        var store = CreateStore(new InMemoryRepository());

        Assert.Equal([C, I, C, M, C, C], store.ImportanceFor("Abra"));
        Assert.Equal([C, M, C, I, C, C], store.ImportanceFor("Machop"));
    }

    [Fact]
    public void SetImportance_saves_notifies_and_survives_a_reload()
    {
        var repository = new InMemoryRepository();
        var store = CreateStore(repository);
        var changes = 0;
        store.Changed += () => changes++;

        store.SetImportance("Abra", [C, C, C, M, C, M]);

        Assert.Equal(1, changes);
        Assert.Equal(1, repository.CaptureSaveCount);
        Assert.Equal([C, C, C, M, C, M], CreateStore(repository).ImportanceFor("Abra"));
    }

    [Fact]
    public void ResetImportance_goes_back_to_the_default()
    {
        var repository = new InMemoryRepository();
        var store = CreateStore(repository);
        store.SetImportance("Abra", [C, C, C, C, C, C]);

        store.ResetImportance("Abra");

        Assert.Equal([C, I, C, M, C, C], store.ImportanceFor("Abra"));
        Assert.Empty(repository.CaptureNotes.Importance);
    }

    [Fact]
    public void Returned_arrays_are_copies()
    {
        var store = CreateStore(new InMemoryRepository());

        store.ImportanceFor("Abra")[1] = M;

        Assert.Equal(I, store.ImportanceFor("Abra")[1]);
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
    public void Comparison_uses_the_current_importance_for_both_sides()
    {
        var store = CreateStore(new InMemoryRepository());
        store.Conclude(Strong);

        Assert.Equal(CaptureComparison.Equivalent, store.CompareWithBest(WeakAttack));

        store.SetImportance("Squirtle", [C, C, C, M, C, C]);

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

    [Fact]
    public void Main_attack_decides_the_machop_case()
    {
        var store = CreateStore(new InMemoryRepository());
        store.Conclude(new CaptureInput("Machop", 16, "Docile", [50, 31, 25, 21, 19, 20]));

        var hasty = new CaptureInput("Machop", 17, "Hasty", [52, 37, 21, 19, 16, 22]);

        Assert.Equal(CaptureComparison.Better, store.CompareWithBest(hasty));
        Assert.NotNull(store.Conclude(hasty));
        Assert.Equal("Hasty", store.BestFor("Machop")!.Nature);
    }

    [Theory]
    [InlineData("fireRed", new[] { 75, 113, 18, 118, 19, 99 })]
    [InlineData("leafGreen", new[] { 75, 47, 102, 52, 103, 63 })]
    public void Deoxys_is_evaluated_with_the_forme_of_the_saved_game_version(string version, int[] stats)
    {
        var repository = new InMemoryRepository();
        repository.SavePreferences(new UiPreferences { GameVersion = version });

        var result = CreateStore(repository).Evaluate(new CaptureInput("Deoxys", 30, "Quirky", stats));

        Assert.DoesNotContain(result.Cells, c => c is IvInconsistent);
    }
}