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

    /// <summary>Deletion records saved with the data (spec 008).</summary>
    IReadOnlyDictionary<string, DateTimeOffset> LoadDeleted();

    SaveResult SaveAll(IReadOnlyDictionary<string, TrainingPokemon> all, IReadOnlyDictionary<string, DateTimeOffset> deleted);

    UiPreferences LoadPreferences();

    void SavePreferences(UiPreferences preferences);

    /// <summary>Capture conveniences (counted stats, best so far); empty when missing or invalid.</summary>
    CaptureNotes LoadCaptureNotes();

    void SaveCaptureNotes(CaptureNotes notes);

    /// <summary>The device's link to external storage, or null when sync is off (spec 008).</summary>
    SyncConnection? LoadSyncConnection();

    void SaveSyncConnection(SyncConnection connection);

    void ClearSyncConnection();
}

/// <summary>Browser features the UI needs besides storage (contracts/ui-shell.md, interop.js).</summary>
public interface IBrowserApi
{
    void SetTheme(string theme);

    void SetLanguage(string language);

    /// <summary>The browser's preferred languages, in order (navigator.languages); empty when unknown.</summary>
    IReadOnlyList<string> Languages();

    void DownloadText(string fileName, string text);
}