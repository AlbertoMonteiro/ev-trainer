using Bunit;

using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;
using EvTrainer.Web.Localization;
using EvTrainer.Web.State;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace EvTrainer.Tests.Support;

/// <summary>bUnit context with the app services wired to in-memory fakes.</summary>
public abstract class AppTestContext : BunitContext
{
    public static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    public static readonly Pokedex Dex = Pokedex.LoadEmbedded();

    protected AppTestContext(LocalizationCatalog? catalog = null)
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        Catalog = catalog ?? LocalizationCatalog.LoadEmbedded();
        Language = new LanguageState(Catalog);
        Store = new TrainerStore(Repository, Data);
        Capture = new CaptureStore(Repository, Dex, Data);
        Toasts = new ToastService(Time);

        Services.AddSingleton(Data);
        Services.AddSingleton(new OpponentSearch(Data));
        Services.AddSingleton(Dex);
        Services.AddSingleton<IPokemonRepository>(Repository);
        Services.AddSingleton<IBrowserApi>(Browser);
        Services.AddSingleton(Store);
        Services.AddSingleton(Capture);
        Services.AddSingleton(Toasts);
        Services.AddSingleton<TimeProvider>(Time);
        Services.AddSingleton(Catalog);
        Services.AddSingleton(Language);
        Services.AddSingleton<IStringLocalizer, JsonStringLocalizer>();
        Services.AddSingleton(typeof(IStringLocalizer<>), typeof(JsonStringLocalizer<>));
    }

    public InMemoryRepository Repository { get; } = new();

    public FakeBrowserApi Browser { get; } = new();

    public ManualTimeProvider Time { get; } = new();

    public LocalizationCatalog Catalog { get; }

    public LanguageState Language { get; }

    public TrainerStore Store { get; }

    public CaptureStore Capture { get; }

    public ToastService Toasts { get; }

    /// <summary>Text of a pt-BR key, for assertions.</summary>
    public string T(string key, params object[] args) =>
        args.Length == 0
            ? new JsonStringLocalizer(Catalog, Language)[key].Value
            : new JsonStringLocalizer(Catalog, Language)[key, args].Value;

    /// <summary>Seeds the repository and loads the store, selecting the first Pokémon.</summary>
    public string Seed(TrainingPokemon pokemon, string id = "p1")
    {
        Repository.Stored[id] = pokemon.Clone();
        Store.Load();
        return id;
    }
}