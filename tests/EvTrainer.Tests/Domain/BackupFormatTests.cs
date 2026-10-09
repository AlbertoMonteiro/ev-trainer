using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Tests.Domain;

public class BackupFormatTests
{
    private static BackupDocuments Ok(BackupReadResult result) => result switch
    {
        BackupDocuments documents => documents,
        BackupError error => throw new Xunit.Sdk.XunitException($"Expected documents, got {error.Kind}"),
    };

    private static BackupErrorKind Error(BackupReadResult result) => result switch
    {
        BackupDocuments => throw new Xunit.Sdk.XunitException("Expected an error"),
        BackupError error => error.Kind,
    };

    [Theory]
    [InlineData("not json", BackupErrorKind.InvalidJson)]
    [InlineData("[1, 2]", BackupErrorKind.InvalidJson)]
    [InlineData("""{ "collection": "other", "documents": {} }""", BackupErrorKind.WrongCollection)]
    [InlineData("""{ "documents": {} }""", BackupErrorKind.WrongCollection)]
    [InlineData("""{ "collection": "pokemon" }""", BackupErrorKind.MissingDocuments)]
    public void Rejects_invalid_files(string json, BackupErrorKind expected) =>
        Assert.Equal(expected, Error(BackupFormat.Read(json)));

    [Fact]
    public void Read_of_Write_is_identity()
    {
        var a = Pokemons.Squirtle().WithEvents(("Bulbasaur (rival)", 1, 1), ("Caterpie", 3, 2), ("Nidoran F", 1, 1));
        a.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [20, 9, 12, 11, 12, 11], Evs = new int[6] });
        a.Pokerus = true;
        var b = Pokemons.Squirtle(nature: "");
        b.Order = 2;
        var all = new Dictionary<string, TrainingPokemon> { ["a"] = a, ["b"] = b };

        var json = BackupFormat.Write(all, indented: true);
        var back = Ok(BackupFormat.Read(json)).Items;

        Assert.Equal(json, BackupFormat.Write(back, indented: true));
    }

    [Fact]
    public void Write_keeps_alias_names_as_recorded()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Bulbasaur (rival)", 1, 1), ("Nidoran F", 1, 1));

        var json = BackupFormat.Write(new Dictionary<string, TrainingPokemon> { ["x"] = pokemon }, indented: true);

        Assert.Contains("\"Bulbasaur (rival)\"", json);
        Assert.Contains("\"Nidoran F\"", json);
    }

    [Fact]
    public void Preview_counts_new_and_replaced_by_id()
    {
        var current = new Dictionary<string, TrainingPokemon> { ["a"] = Pokemons.Squirtle(), ["c"] = Pokemons.Squirtle() };
        var incoming = new Dictionary<string, TrainingPokemon> { ["a"] = Pokemons.Squirtle(), ["b"] = Pokemons.Squirtle() };

        Assert.Equal(new BackupMergePreview(New: 1, Replaced: 1), BackupFormat.Preview(current, incoming));
    }
}