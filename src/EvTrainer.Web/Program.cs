using EvTrainer.Web;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.Localization;
using EvTrainer.Web.State;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(_ => ReferenceData.LoadEmbedded());
builder.Services.AddSingleton(_ => LocationNames.LoadEmbedded());
builder.Services.AddSingleton<OpponentSearch>();
builder.Services.AddSingleton(_ => Pokedex.LoadEmbedded());
builder.Services.AddSingleton(_ => VsSeekerGuide.LoadEmbedded());
builder.Services.AddSingleton<TrainingGuide>();
builder.Services.AddSingleton<IPokemonRepository, LocalStorageRepository>();
builder.Services.AddSingleton<IBrowserApi, BrowserApi>();
builder.Services.AddSingleton<TrainerStore>();
builder.Services.AddSingleton<CaptureStore>();
builder.Services.AddSingleton<ISyncProvider>(_ => new GistSyncProvider(new HttpClient { BaseAddress = new Uri(GistSyncProvider.ApiBase) }));
builder.Services.AddSingleton<SyncService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton(_ => LocalizationCatalog.LoadEmbedded());
builder.Services.AddSingleton<LanguageState>();
builder.Services.AddSingleton<IStringLocalizer, JsonStringLocalizer>();
builder.Services.AddSingleton(typeof(IStringLocalizer<>), typeof(JsonStringLocalizer<>));

var host = builder.Build();

var store = host.Services.GetRequiredService<TrainerStore>();
store.Load();
// Language (spec 011): the choice saved from the menu, otherwise the browser language (not saved).
var languageState = host.Services.GetRequiredService<LanguageState>();
var browser = host.Services.GetRequiredService<IBrowserApi>();
var language = LanguageMatcher.Resolve(store.Preferences.Language, languageState.Available, browser.Languages());
languageState.Current = language;
browser.SetLanguage(language);

// Sync (spec 008): sync on start and whenever the page becomes visible again; a failure only leaves it pending.
var sync = host.Services.GetRequiredService<SyncService>();
try
{
    ((IJSInProcessRuntime)host.Services.GetRequiredService<IJSRuntime>()).InvokeVoid("evt.onVisible", DotNetObjectReference.Create(sync));
}
catch (JSException)
{
    // Without the visibility hook the app still syncs on start, after changes and on demand.
}

_ = sync.StartAsync();

await host.RunAsync();