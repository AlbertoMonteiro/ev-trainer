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

    private IJSInProcessRuntime Js => (IJSInProcessRuntime)js;

    public IReadOnlyDictionary<string, TrainingPokemon> LoadAll() =>
        Get(DataKey) is { } json ? PokemonJson.Deserialize(json) ?? [] : new Dictionary<string, TrainingPokemon>();

    public SaveResult SaveAll(IReadOnlyDictionary<string, TrainingPokemon> all) =>
        Set(DataKey, PokemonJson.Serialize(all, indented: false)) ? SaveResult.Saved : SaveResult.Failed;

    public UiPreferences LoadPreferences() => PokemonJson.DeserializePreferences(Get(PreferencesKey));

    public void SavePreferences(UiPreferences preferences) => Set(PreferencesKey, PokemonJson.SerializePreferences(preferences));

    public CaptureNotes LoadCaptureNotes() => CaptureNotesJson.Deserialize(Get(CaptureKey), dex, data);

    public void SaveCaptureNotes(CaptureNotes notes) => Set(CaptureKey, CaptureNotesJson.Serialize(notes));

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