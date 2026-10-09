using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Tests.Domain;

/// <summary>
/// SC-003: the migrated user-data-export.json must give exactly the same numbers as the current app.
/// Expected values were produced by running app-atual.html's own evTotals/countOf/ivRange on the file.
/// The file holds personal data and is git-ignored, so these tests are skipped where it is absent (CI).
/// </summary>
public partial class LegacyExportGoldenTests
{
    private static readonly ReferenceData Data = ReferenceData.LoadEmbedded();

    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "user-data-export.json");

    private static TrainingPokemon Squirtle()
    {
        Assert.SkipUnless(File.Exists(FixturePath), "user-data-export.json is personal data and not in the repository.");
        var json = File.ReadAllText(FixturePath);
        return BackupFormat.Read(json) switch
        {
            BackupDocuments documents => documents.Items["squirtle"],
            BackupError error => throw new Xunit.Sdk.XunitException(error.Kind.ToString()),
        };
    }

    [Fact]
    public void Ev_totals_match_the_current_app()
    {
        var totals = Evs.Totals(Squirtle().Events, Data);

        Assert.Equal([42, 0, 2, 33, 1, 4], totals);
        Assert.Equal(82, totals.Sum());
    }

    [Fact]
    public void Final_iv_ranges_match_the_current_app()
    {
        var final = Ivs.Final(Squirtle(), Data);

        Assert.Equal(["25", "20-21", "18", "27", "29", "30"], final.Select(IvTests.Show));
    }

    [Fact]
    public void Level_23_row_matches_the_current_app()
    {
        var pokemon = Squirtle();
        var row = pokemon.IvLog.Single(l => l.Level == 23);

        Assert.Equal("Wartortle", row.Species);
        Assert.Equal(["25-28", "18-21", "14-18", "23-27", "27-31", "27-30"], Ivs.RangeFor(row, pokemon, Data).Select(IvTests.Show));
    }

    [Theory]
    [InlineData("Bulbasaur", 2)]
    [InlineData("Caterpie", 33)]
    [InlineData("Pikachu", 1)]
    [InlineData("Weedle", 1)]
    [InlineData("Geodude", 1)]
    [InlineData("Onix", 1)]
    [InlineData("Nidoran♀", 3)]
    [InlineData("Jigglypuff", 2)]
    [InlineData("Clefairy", 1)]
    [InlineData("Magnemite", 1)]
    [InlineData("Oddish", 18)]
    [InlineData("Abra", 7)]
    [InlineData("Pidgey", 1)]
    [InlineData("Ivysaur", 1)]
    [InlineData("Horsea", 4)]
    public void Counts_per_opponent_match_the_current_app(string opponent, int expected) =>
        Assert.Equal(expected, Evs.CountFor(Squirtle().Events, opponent, Data));
}