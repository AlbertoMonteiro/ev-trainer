using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.Localization;
using EvTrainer.Web.State;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace EvTrainer.Web.Components;

/// <summary>Base component that re-renders when the store or the UI language changes.</summary>
public abstract class StoreAwareComponent : ComponentBase, IDisposable
{
    [Inject]
    protected TrainerStore Store { get; set; } = default!;

    [Inject]
    protected LanguageState Language { get; set; } = default!;

    [Inject]
    protected IStringLocalizer<App> L { get; set; } = default!;

    protected override void OnInitialized()
    {
        Store.Changed += OnChanged;
        Language.Changed += OnChanged;
    }

    /// <summary>Short stat label (HP, ATK, …) in the active language.</summary>
    protected string StatShort(int index) => L[$"Stat.Short.{index}"];

    /// <summary>How a Pokémon is named on screen: its current species, or a placeholder when it has none.</summary>
    protected string PokemonLabel(TrainingPokemon pokemon) =>
        string.IsNullOrWhiteSpace(pokemon.CurrentSpecies) ? L["Pokemon.NoSpecies"] : pokemon.CurrentSpecies;

    /// <summary>"Timid (+SPE −ATK)" style label of a nature in the active language.</summary>
    protected string NatureLabel(Nature nature) => L[
        "Settings.Nature.Option",
        nature.Name,
        nature is { Increases: { } up, Decreases: { } down }
            ? L["Settings.Nature.Effect", StatShort(up), StatShort(down)].Value
            : L["Settings.Nature.Neutral"].Value];

    private void OnChanged() => InvokeAsync(StateHasChanged);

    protected virtual void OnDispose()
    {
    }

    public void Dispose()
    {
        Store.Changed -= OnChanged;
        Language.Changed -= OnChanged;
        OnDispose();
        GC.SuppressFinalize(this);
    }
}