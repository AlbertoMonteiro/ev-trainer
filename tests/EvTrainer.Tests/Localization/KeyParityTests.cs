using System.Text.RegularExpressions;

using EvTrainer.Web.Localization;

namespace EvTrainer.Tests.Localization;

public partial class KeyParityTests
{
    private static readonly LocalizationCatalog Catalog = LocalizationCatalog.LoadEmbedded();

    [Fact]
    public void Every_language_has_exactly_the_pt_BR_keys_and_placeholders()
    {
        var reference = Catalog.Strings(LocalizationCatalog.DefaultLanguage);
        foreach (var language in Catalog.Languages)
        {
            var strings = Catalog.Strings(language);
            Assert.Equal(reference.Keys.Order(StringComparer.Ordinal), strings.Keys.Order(StringComparer.Ordinal));
            foreach (var (key, text) in reference)
            {
                Assert.True(
                    Placeholders(text).SetEquals(Placeholders(strings[key])),
                    $"Placeholders differ for '{key}' in '{language}'.");
            }
        }
    }

    [Fact]
    public void Every_key_used_in_the_source_exists_in_pt_BR()
    {
        var reference = Catalog.Strings(LocalizationCatalog.DefaultLanguage);
        var sourceDir = Path.Combine(FindRepositoryRoot(), "src", "EvTrainer.Web");
        var used = Directory.EnumerateFiles(sourceDir, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".razor", StringComparison.Ordinal) || f.EndsWith(".cs", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(f => KeyUsage().Matches(File.ReadAllText(f)).Select(m => (File: Path.GetFileName(f), Key: m.Groups["key"].Value)))
            .ToList();

        Assert.NotEmpty(used);
        var missing = used.Where(u => !reference.ContainsKey(u.Key)).Select(u => $"{u.File}: {u.Key}").Distinct().ToList();
        Assert.True(missing.Count == 0, "Missing keys: " + string.Join(", ", missing));
    }

    private static HashSet<string> Placeholders(string text) =>
        [.. PlaceholderPattern().Matches(text).Select(m => m.Value)];

    internal static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PokemonEvTrainer.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"\bL\[""(?<key>[A-Za-z0-9_.]+)""")]
    private static partial Regex KeyUsage();
}