using EvTrainer.Tests.Localization;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

public class LocationNamesTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();
    private static readonly LocationNames Names = LocationNames.LoadEmbedded();

    private static IEnumerable<string> AllPlaces() =>
    [
        .. Data.TrainersByLocation.Select(l => l.Name),
        .. Data.Opponents.Select(o => o.WildLocation).Where(w => w.Length > 0),
        .. Data.WildPlaces(GameVersion.FireRed).Values.SelectMany(p => p),
        .. Data.WildPlaces(GameVersion.LeafGreen).Values.SelectMany(p => p),
    ];

    [Fact]
    public void Every_reference_place_has_an_english_name()
    {
        var missing = AllPlaces().Distinct().Where(p => !Names.HasTranslation(p)).ToList();

        Assert.True(missing.Count == 0, "Missing English names: " + string.Join(" | ", missing));
    }

    [Fact]
    public void English_names_have_no_portuguese_words()
    {
        var offending = AllPlaces().Distinct()
            .Select(p => Names.Display(p, "en"))
            .Where(t => KeyParityTests.PortugueseWords.Any(w => KeyParityTests.ContainsWord(t, w)) || t.Contains(" em ", StringComparison.Ordinal))
            .ToList();

        Assert.True(offending.Count == 0, "Portuguese in English place names: " + string.Join(" | ", offending));
    }

    [Theory]
    [InlineData("Rota 22", "en", "Route 22")]
    [InlineData("Ginásio de Pewter (Brock)", "en", "Pewter Gym (Brock)")]
    [InlineData("Laboratório do Prof. Oak", "en", "Prof. Oak's Lab")]
    [InlineData("Rota 22", "pt-BR", "Rota 22")]
    [InlineData("Nowhere", "en", "Nowhere")]
    public void Displays_the_place_in_the_active_language(string place, string language, string expected) =>
        Assert.Equal(expected, Names.Display(place, language));
}