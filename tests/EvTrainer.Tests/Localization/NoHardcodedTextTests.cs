using System.Text.RegularExpressions;

using AngleSharp.Dom;

using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Localization;
using EvTrainer.Web.Pages;

namespace EvTrainer.Tests.Localization;

/// <summary>
/// SC-010: with a synthetic language where every key renders as «key», no visible text or accessible
/// label may be left outside that pattern, except game data (names, locations), numbers and symbols.
/// </summary>
public partial class NoHardcodedTextTests : AppTestContext
{
    private static readonly LocalizationCatalog Synthetic = new(new Dictionary<string, IReadOnlyDictionary<string, string>>
    {
        ["pt-BR"] = LocalizationCatalog.LoadEmbedded().Strings("pt-BR"),
        ["xx"] = LocalizationCatalog.LoadEmbedded().Strings("pt-BR").ToDictionary(kv => kv.Key, kv => $"«{kv.Key}»"),
    });

    private readonly HashSet<string> _data;

    public NoHardcodedTextTests()
        : base(Synthetic)
    {
        _data =
        [
            .. Data.Opponents.Select(o => o.Name),
            .. Data.Natures.Select(n => n.Name),
            .. Data.TrainersByLocation.Select(l => l.Name),
            .. Dex.Species.Select(s => s.Name),
            // Sprite badges without a dex number show the initial of a data name (vitamins, unknown species).
            .. Data.Opponents.Select(o => o.Name[..1].ToUpperInvariant()),
            // Each language is listed by its own name (endonym), taken from its resource file.
            .. Synthetic.Languages.Select(l => LocalizationCatalog.LoadEmbedded().Find(l, "Language.Name") ?? Synthetic.Find(l, "Language.Name")!),
        ];
        Language.Current = "xx";
    }

    [Fact]
    public void Empty_state_has_no_hardcoded_text()
    {
        Store.Load();

        var cut = Render<Home>();

        AssertNoHardcodedText(cut.Nodes);
    }

    [Fact]
    public void Every_tab_and_the_menu_have_no_hardcoded_text()
    {
        var pokemon = Pokemons.Squirtle().WithEvents(("Caterpie", 3, 1), ("Abra", 1, 2));
        pokemon.IvLog.Add(new LevelEntry { Level = 5, Species = "Squirtle", Stats = [99, 9, 12, 11, 12, 11], Evs = new int[6] });
        Seed(pokemon);
        var cut = Render<Home>();

        for (var tab = 0; tab < 3; tab++)
        {
            cut.FindAll("[role=tab]")[tab].Click();
            AssertNoHardcodedText(cut.Nodes);
        }

        cut.Find("header button.menu").Click();
        AssertNoHardcodedText(cut.Nodes);

        cut.FindAll("[role=tab]")[0].Click();
        cut.Find("button.opponent[data-opp='Caterpie']").Click();
        AssertNoHardcodedText(cut.Nodes);
    }

    private void AssertNoHardcodedText(INodeList nodes)
    {
        var offenders = new List<string>();
        foreach (var node in nodes.SelectMany(n => n.GetDescendantsAndSelf()))
        {
            if (node is IText text && text.ParentElement?.LocalName is not ("style" or "script"))
            {
                Check(text.Data, $"text in <{text.ParentElement?.LocalName}>", offenders);
            }

            if (node is IElement element)
            {
                foreach (var attribute in new[] { "aria-label", "title", "placeholder" })
                {
                    if (element.GetAttribute(attribute) is { } value)
                    {
                        Check(value, $"{attribute} of <{element.LocalName}>", offenders);
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, "Hardcoded text: " + string.Join(" | ", offenders.Distinct()));
    }

    private void Check(string raw, string where, List<string> offenders)
    {
        var value = raw.Trim();
        if (value.Length == 0
            || Localized().IsMatch(value)
            || NumbersAndSymbols().IsMatch(value)
            || _data.Contains(value)
            || value.Split(" ", StringSplitOptions.RemoveEmptyEntries).All(word => Localized().IsMatch(word) || NumbersAndSymbols().IsMatch(word) || _data.Contains(word)))
        {
            return;
        }

        offenders.Add($"{where}: \"{value}\"");
    }

    [GeneratedRegex(@"^(«[A-Za-z0-9_.]+»\s*)+$")]
    private static partial Regex Localized();

    [GeneratedRegex(@"^[\d\s/–\-·×+#●☰()📱🔄⚠️🗙️]*$")]
    private static partial Regex NumbersAndSymbols();
}