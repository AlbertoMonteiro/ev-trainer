using System.Reflection;

namespace EvTrainer.Web;

/// <summary>Version of the running build, shown in the menu so users can tell which deploy they have.</summary>
public static class AppInfo
{
    private const int ShortCommitLength = 7;

    /// <summary>"1.0.0 (730e0b5)": assembly version plus the short git commit added by the SDK.</summary>
    public static string DisplayVersion { get; } = Format(
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty);

    public static string Format(string informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return "?";
        }

        var plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (plus < 0)
        {
            return informationalVersion;
        }

        var commit = informationalVersion[(plus + 1)..];
        return $"{informationalVersion[..plus]} ({commit[..Math.Min(ShortCommitLength, commit.Length)]})";
    }
}