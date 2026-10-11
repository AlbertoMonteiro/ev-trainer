using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.State;

public enum SaveResult
{
    Saved,
    Failed,
}

/// <summary>Storage boundary (contracts/ui-shell.md). Writes are immediate.</summary>
public interface IPokemonRepository
{
    IReadOnlyDictionary<string, TrainingPokemon> LoadAll();

    SaveResult SaveAll(IReadOnlyDictionary<string, TrainingPokemon> all);

    UiPreferences LoadPreferences();

    void SavePreferences(UiPreferences preferences);

    /// <summary>Capture conveniences (counted stats, best so far); empty when missing or invalid.</summary>
    CaptureNotes LoadCaptureNotes();

    void SaveCaptureNotes(CaptureNotes notes);
}

/// <summary>Browser features the UI needs besides storage (contracts/ui-shell.md, interop.js).</summary>
public interface IBrowserApi
{
    void SetTheme(string theme);

    void SetLanguage(string language);

    void DownloadText(string fileName, string text);
}