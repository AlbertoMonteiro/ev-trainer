using Bunit;

using EvTrainer.Tests.Support;
using EvTrainer.Web.Components.Settings;
using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Model;

using Microsoft.AspNetCore.Components.Forms;

namespace EvTrainer.Tests.Components.Settings;

public class BackupPanelTests : AppTestContext
{
    /// <summary>A backup in the user-data-export.json format (same "squirtle" id and alias names).</summary>
    private static string LegacyExport() =>
        BackupFormat.Write(
            new Dictionary<string, TrainingPokemon>
            {
                ["squirtle"] = Pokemons.Squirtle().WithEvents(("Bulbasaur (rival)", 1, 1), ("Caterpie", 8, 1), ("Nidoran F", 1, 1)),
            },
            indented: true);

    [Fact]
    public void Export_downloads_a_dated_indented_backup()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<BackupPanel>();

        cut.Find("button.export").Click();

        var (fileName, text) = Assert.Single(Browser.Downloads);
        Assert.Equal("evs-trainer-backup-2026-10-09.json", fileName);
        Assert.Contains("\n", text);
        Assert.Contains("\"collection\": \"pokemon\"", text);
        Assert.Equal(T("Backup.Exported"), Toasts.Message);
    }

    [Fact]
    public void Import_shows_preview_and_only_saves_after_confirmation()
    {
        Store.Load();
        var cut = Render<BackupPanel>();

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(LegacyExport(), "user-data-export.json"));

        cut.WaitForAssertion(() => Assert.Equal(T("Backup.ImportPreview", 1, 1, 0), cut.Find(".import-preview").TextContent.Trim()));
        Assert.Empty(Repository.Stored);

        cut.Find("button.confirm-import").Click();

        Assert.True(Repository.Stored.ContainsKey("squirtle"));
        Assert.Equal("squirtle", Store.SelectedId);
        Assert.Equal(T("Backup.Imported", 1, 0), Toasts.Message);
    }

    [Fact]
    public void Invalid_file_shows_an_error_and_changes_nothing()
    {
        Seed(Pokemons.Squirtle());
        var cut = Render<BackupPanel>();

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("{ nope", "x.json"));

        cut.WaitForAssertion(() => Assert.Equal(T("Backup.Error.InvalidJson"), cut.Find(".error").TextContent.Trim()));
        Assert.Empty(cut.FindAll("button.confirm-import"));
        Assert.Equal(["p1"], Repository.Stored.Keys);
    }

    [Fact]
    public void Shows_the_local_only_notice()
    {
        Seed(Pokemons.Squirtle());

        var cut = Render<BackupPanel>();

        Assert.Equal(T("Backup.LocalOnlyNotice"), cut.Find(".hint").TextContent.Trim());
    }
}