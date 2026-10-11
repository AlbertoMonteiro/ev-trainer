using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

using Microsoft.JSInterop;

namespace EvTrainer.Web.State;

/// <summary>Stores everything in <c>localStorage</c>, in the same format as the backup file.</summary>
public sealed class LocalStorageRepository(IJSRuntime js, Pokedex dex, ReferenceData data) : IPokemonRepository
{
    public const string DataKey = "evtrainer:v1:data";
    public const string PreferencesKey = "evtrainer:v1:prefs";
    public const string CaptureKey = "evtrainer:v1:capture";
    public const string SyncKey = "evtrainer:v1:sync";

    private IJSInProcessRuntime Js => (IJSInProcessRuntime)js;

    public IReadOnlyDictionary<string, TrainingPokemon> LoadAll() =>
        Get(DataKey) is { } json ? PokemonJson.Deserialize(json) ?? [] : new Dictionary<string, TrainingPokemon>();

    public IReadOnlyDictionary<string, DateTimeOffset> LoadDeleted() =>
        Get(DataKey) is { } json && PokemonJson.DeserializeSnapshot(json) is { } snapshot
            ? snapshot.Deleted
            : new Dictionary<string, DateTimeOffset>();

    public SaveResult SaveAll(IReadOnlyDictionary<string, TrainingPokemon> all, IReadOnlyDictionary<string, DateTimeOffset> deleted) =>
        Set(DataKey, PokemonJson.Serialize(all, indented: false, deleted)) ? SaveResult.Saved : SaveResult.Failed;

    public UiPreferences LoadPreferences() => PokemonJson.DeserializePreferences(Get(PreferencesKey));

    public void SavePreferences(UiPreferences preferences) => Set(PreferencesKey, PokemonJson.SerializePreferences(preferences));

    public CaptureNotes LoadCaptureNotes() => CaptureNotesJson.Deserialize(Get(CaptureKey), dex, data);

    public void SaveCaptureNotes(CaptureNotes notes) => Set(CaptureKey, CaptureNotesJson.Serialize(notes));

    public SyncConnection? LoadSyncConnection() => SyncConnectionJson.Deserialize(Get(SyncKey));

    public void SaveSyncConnection(SyncConnection connection) => Set(SyncKey, SyncConnectionJson.Serialize(connection));

    public void ClearSyncConnection() => Call("evt.storageRemove", SyncKey);

    private string? Get(string key)
    {
        try
        {
            return Js.Invoke<string?>("evt.storageGet", key);
        }
        catch (JSException)
        {
            return null;
        }
    }

    private void Call(string identifier, params object?[] args)
    {
        try
        {
            Js.InvokeVoid(identifier, args);
        }
        catch (JSException)
        {
            // Nothing else to do: the next load treats a broken value as "not connected".
        }
    }

    private bool Set(string key, string value)
    {
        try
        {
            return Js.Invoke<bool>("evt.storageSet", key, value);
        }
        catch (JSException)
        {
            return false;
        }
    }
}

public sealed class BrowserApi(IJSRuntime js) : IBrowserApi
{
    private IJSInProcessRuntime Js => (IJSInProcessRuntime)js;

    public void SetTheme(string theme) => Call("evt.setTheme", theme);

    public void SetLanguage(string language) => Call("evt.setLanguage", language);

    public void DownloadText(string fileName, string text) => Call("evt.downloadText", fileName, text);

    private void Call(string identifier, params object?[] args)
    {
        try
        {
            Js.InvokeVoid(identifier, args);
        }
        catch (JSException)
        {
            // Cosmetic browser features (theme, lang, download) must never break the app.
        }
    }
}