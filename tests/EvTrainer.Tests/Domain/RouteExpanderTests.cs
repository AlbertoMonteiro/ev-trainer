using EvTrainer.Web.Domain;

namespace EvTrainer.Tests.Domain;

public class RouteExpanderTests
{
    [Theory]
    [InlineData("rota 3, 4, 22", "rota 3, rota 4, rota 22")]
    [InlineData("rotas 12–15", "rota 12, rota 13, rota 14, rota 15")]
    [InlineData("rota 5-8", "rota 5, rota 6, rota 7, rota 8")]
    [InlineData("rota 1, 2, 24, 25; rival rota 22", "rota 1, rota 2, rota 24, rota 25; rival rota 22")]
    [InlineData("rotas 12–15, safari", "rota 12, rota 13, rota 14, rota 15, safari")]
    [InlineData("mt. moon; brock", "mt. moon; brock")]
    [InlineData("rota 24, 25 (foge)", "rota 24, rota 25 (foge)")]
    [InlineData("route 3, 4, 22", "route 3, route 4, route 22")]
    [InlineData("routes 12–15", "route 12, route 13, route 14, route 15")]
    [InlineData("route 24, 25 (flees); rival in cerulean", "route 24, route 25 (flees); rival in cerulean")]
    public void Expands_route_lists_and_ranges(string input, string expected) =>
        Assert.Equal(expected, RouteExpander.Expand(input));
}