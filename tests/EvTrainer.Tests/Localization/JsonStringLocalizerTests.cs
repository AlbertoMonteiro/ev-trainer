using EvTrainer.Web.Localization;

namespace EvTrainer.Tests.Localization;

public class JsonStringLocalizerTests
{
    private static LocalizationCatalog Catalog() => new(new Dictionary<string, IReadOnlyDictionary<string, string>>
    {
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["Hello"] = "Olá",
            ["Gain"] = "{0}: +{1}",
            ["OnlyPt"] = "Só em português",
        },
        ["en"] = new Dictionary<string, string>
        {
            ["Hello"] = "Hello",
            ["Gain"] = "{0}: +{1}",
        },
    });

    [Fact]
    public void Uses_the_active_language()
    {
        var state = new LanguageState(Catalog());
        var localizer = new JsonStringLocalizer(Catalog(), state);

        Assert.Equal("Olá", localizer["Hello"].Value);
        state.Current = "en";
        Assert.Equal("Hello", localizer["Hello"].Value);
    }

    [Fact]
    public void Formats_arguments_with_invariant_culture()
    {
        var localizer = new JsonStringLocalizer(Catalog(), new LanguageState(Catalog()));

        Assert.Equal("Caterpie: +1000", localizer["Gain", "Caterpie", 1000].Value);
    }

    [Fact]
    public void Falls_back_to_pt_BR_then_to_bracketed_key()
    {
        var state = new LanguageState(Catalog()) { Current = "en" };
        var localizer = new JsonStringLocalizer(Catalog(), state);

        Assert.Equal("Só em português", localizer["OnlyPt"].Value);
        var missing = localizer["Nope"];
        Assert.Equal("[Nope]", missing.Value);
        Assert.True(missing.ResourceNotFound);
    }

    [Fact]
    public void Language_state_lists_available_languages_and_raises_changed()
    {
        var state = new LanguageState(Catalog());
        var raised = 0;
        state.Changed += () => raised++;

        Assert.Equal("pt-BR", state.Current);
        Assert.Equal(["pt-BR", "en"], state.Available);
        state.Current = "en";
        state.Current = "en";
        state.Current = "zz";

        Assert.Equal(1, raised);
        Assert.Equal("en", state.Current);
    }

    [Fact]
    public void Embedded_catalog_has_pt_BR_first_then_en()
    {
        var catalog = LocalizationCatalog.LoadEmbedded();

        Assert.Equal(["pt-BR", "en"], catalog.Languages);
        Assert.Equal("EVs Trainer", catalog.Find("pt-BR", "App.Title"));
        Assert.Equal("English", catalog.Find("en", "Language.Name"));
        Assert.Equal("EVs Trainer", catalog.Find("en", "App.Title"));
    }
}