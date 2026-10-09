using EvTrainer.Web;

namespace EvTrainer.Tests.State;

public class AppInfoTests
{
    [Theory]
    [InlineData("1.0.0+730e0b5c1d2e3f4a5b6c7d8e9f001122334455", "1.0.0 (730e0b5)")]
    [InlineData("1.0.0+abc", "1.0.0 (abc)")]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("", "?")]
    public void Formats_version_with_short_commit(string informational, string expected) =>
        Assert.Equal(expected, AppInfo.Format(informational));

    [Fact]
    public void Reads_the_assembly_version() => Assert.False(string.IsNullOrWhiteSpace(AppInfo.DisplayVersion));
}