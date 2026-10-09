using EvTrainer.Web.Domain;

namespace EvTrainer.Tests.Domain;

public class TextFoldTests
{
    [Theory]
    [InlineData("Laboratório do Prof. Oak", "laboratorio do prof. oak")]
    [InlineData("Ginásio de Pewter (Brock)", "ginasio de pewter (brock)")]
    [InlineData("Nidoran♀", "nidoran f")]
    [InlineData("Nidoran♂", "nidoran m")]
    [InlineData("Ação Ãõê Ü ç à â í ú", "acao aoe u c a a i u")]
    [InlineData("Farfetch'd", "farfetch'd")]
    [InlineData("ROTA 3", "rota 3")]
    public void Folds_accents_symbols_and_case(string input, string expected) =>
        Assert.Equal(expected, TextFold.Fold(input));
}