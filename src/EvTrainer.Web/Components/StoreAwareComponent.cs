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