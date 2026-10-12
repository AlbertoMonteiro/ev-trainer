using EvTrainer.Web.Domain;

namespace EvTrainer.Tests.Domain;

public class LanguageMatcherTests
{
    private static readonly string[] Available = ["pt-BR", "en"];

    [Theory]
    [InlineData("en", "en-US", "pt-BR")]
    [InlineData("pt-BR", "pt-PT")]
    [InlineData("pt-BR", "es", "pt-BR", "en")]
    [InlineData("en", "fr", "ja")]
    [InlineData("en")]
    [InlineData("en", "EN-gb")]
    [InlineData("pt-BR", "pt")]
    [InlineData("pt-BR", "", "  ", "pt-br")]
    public void Picks_the_first_supported_browser_language_or_english(string expected, params string[] browser) =>
        Assert.Equal(expected, LanguageMatcher.Pick(browser));

    [Fact]
    public void A_saved_language_wins_over_the_browser() =>
        Assert.Equal("pt-BR", LanguageMatcher.Resolve("pt-BR", Available, ["en-US"]));

    [Fact]
    public void A_manual_choice_of_english_wins_over_a_portuguese_browser() =>
        Assert.Equal("en", LanguageMatcher.Resolve("en", Available, ["pt-BR"]));

    [Theory]
    [InlineData(null)]
    [InlineData("de")]
    [InlineData("")]
    public void Without_a_usable_saved_language_follows_the_browser(string? saved)
    {
        Assert.Equal("en", LanguageMatcher.Resolve(saved, Available, ["en-US"]));
        Assert.Equal("pt-BR", LanguageMatcher.Resolve(saved, Available, ["pt-BR"]));
    }
}