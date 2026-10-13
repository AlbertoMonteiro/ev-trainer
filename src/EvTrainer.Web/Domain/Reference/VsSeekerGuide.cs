using System.Text.Json;

namespace EvTrainer.Web.Domain.Reference;

/// <summary>
/// One team of a trainer the VS Seeker can rematch (spec 013). <see cref="Index"/> is the game's rematch stage: 0 the
/// original battle, 1 with the VS Seeker, 2 after visiting Celadon, 3 after Fuchsia, 4 after the Elite Four, 5 after
/// the Ruby/Sapphire link (end of the Sevii Islands).
/// </summary>
public sealed record GuideStage(int Index, IReadOnlyList<(string Species, int Level)> Team)
{
    public bool RequiresEliteFour => Index >= 4;
}

/// <summary>A Kanto trainer the VS Seeker can rematch, with every stage of its team in order.</summary>
public sealed record GuideTrainer(
    string Id,
    string TrainerClass,
    string Name,
    string Location,
    bool Double,
    IReadOnlyList<string> Access,
    IReadOnlyList<GuideStage> Stages);

/// <summary>The embedded <c>vs-seeker-guide.json</c>, generated from the FireRed decompilation.</summary>
public sealed class VsSeekerGuide(IReadOnlyList<GuideTrainer> trainers)
{
    private const string ResourceName = "EvTrainer.vs-seeker-guide.json";

    public IReadOnlyList<GuideTrainer> Trainers { get; } = trainers;

    public static VsSeekerGuide LoadEmbedded()
    {
        using var stream = typeof(VsSeekerGuide).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");
        using var document = JsonDocument.Parse(stream);
        return new VsSeekerGuide([
            .. document.RootElement.GetProperty("trainers").EnumerateArray().Select(t => new GuideTrainer(
                t.GetProperty("id").GetString()!,
                t.GetProperty("class").GetString()!,
                t.GetProperty("name").GetString()!,
                t.GetProperty("location").GetString()!,
                t.GetProperty("double").GetBoolean(),
                [.. t.GetProperty("access").EnumerateArray().Select(a => a.GetString()!)],
                [
                    .. t.GetProperty("stages").EnumerateArray().Select(s => new GuideStage(
                        s.GetProperty("index").GetInt32(),
                        [.. s.GetProperty("team").EnumerateArray().Select(p => (p[0].GetString()!, p[1].GetInt32()))])),
                ])),
        ]);
    }
}