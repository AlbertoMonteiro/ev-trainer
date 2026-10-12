using EvTrainer.Web.State;

namespace EvTrainer.Tests.Support;

public sealed class FakeBrowserApi : IBrowserApi
{
    public string? Theme { get; private set; }
    public string? Language { get; private set; }
    public List<(string FileName, string Text)> Downloads { get; } = [];
    public List<string> BrowserLanguages { get; } = ["pt-BR"];

    public void SetTheme(string theme) => Theme = theme;

    public void SetLanguage(string language) => Language = language;

    public IReadOnlyList<string> Languages() => BrowserLanguages;

    public void DownloadText(string fileName, string text) => Downloads.Add((fileName, text));
}