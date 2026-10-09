using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Settings;

namespace EvTrainer.Tests.Components.Settings;

public class NatureOptionsTests : AppTestContext
{
    [Fact]
    public void Options_show_the_effect_or_neutral()
    {
        Seed(Pokemons.Squirtle());

        var cut = Render<SettingsForm>();

        var timid = cut.Find("select#settings-nature option[value='Timid']").TextContent.Trim();
        var hardy = cut.Find("select#settings-nature option[value='Hardy']").TextContent.Trim();
        Assert.Equal(T("Settings.Nature.Option", "Timid", T("Settings.Nature.Effect", T("Stat.Short.5"), T("Stat.Short.1"))), timid);
        Assert.Equal(T("Settings.Nature.Option", "Hardy", T("Settings.Nature.Neutral")), hardy);
    }
}