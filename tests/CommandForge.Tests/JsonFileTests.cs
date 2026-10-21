using CommandForge.Application.Logging;
using CommandForge.Application.Settings;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.Persistence;

namespace CommandForge.Tests;

/// <summary>Tests crash-safe JSON persistence and compatibility with files written by v1.5.1.</summary>
public sealed class JsonFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"cf-jsonfile-{Guid.NewGuid():N}");

    public JsonFileTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Read_MissingFile_ReturnsNull()
        => Assert.Null(JsonFile.Read(Path.Combine(_directory, "missing.json"), SettingsJsonContext.Default.SettingsFileModel));

    [Fact]
    public void Read_CorruptFile_KeepsACopyAndReturnsNull()
    {
        var path = Path.Combine(_directory, "config.json");
        File.WriteAllText(path, "{ \"Theme\": \"Dark\", ");

        var model = JsonFile.Read(path, SettingsJsonContext.Default.SettingsFileModel);

        Assert.Null(model);
        Assert.False(File.Exists(path));
        var backup = Assert.Single(Directory.GetFiles(_directory, "config.corrupt-*.json"));
        Assert.Equal("{ \"Theme\": \"Dark\", ", File.ReadAllText(backup));
    }

    [Fact]
    public void Read_CorruptFile_WithoutBackup_LeavesItInPlace()
    {
        var path = Path.Combine(_directory, "settings-export.json");
        File.WriteAllText(path, "not json");

        Assert.Null(JsonFile.Read(path, SettingsJsonContext.Default.SettingsFileModel, keepCorruptCopy: false));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Read_LockedFile_ReturnsNullInsteadOfThrowing()
    {
        var path = Path.Combine(_directory, "config.json");
        File.WriteAllText(path, "{}");

        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Null(JsonFile.Read(path, SettingsJsonContext.Default.SettingsFileModel));
        }

        Assert.True(File.Exists(path)); // a read failure is not corruption: nothing is renamed
    }

    [Fact]
    public void Write_ReplacesTheFileAndLeavesNoTemporaryFiles()
    {
        var path = Path.Combine(_directory, "nested", "config.json");

        JsonFile.Write(path, new SettingsFileModel { Theme = AppTheme.Light }, SettingsJsonContext.Default.SettingsFileModel);
        JsonFile.Write(path, new SettingsFileModel { Theme = AppTheme.Dark }, SettingsJsonContext.Default.SettingsFileModel);

        Assert.Equal(AppTheme.Dark, JsonFile.Read(path, SettingsJsonContext.Default.SettingsFileModel)!.Theme);
        Assert.Equal([path], Directory.GetFiles(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public void TryWrite_WhenTheTargetIsLocked_ReturnsFalseAndKeepsTheOldContent()
    {
        var path = Path.Combine(_directory, "config.json");
        JsonFile.Write(path, new SettingsFileModel { Theme = AppTheme.Light }, SettingsJsonContext.Default.SettingsFileModel);

        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.False(JsonFile.TryWrite(path, new SettingsFileModel { Theme = AppTheme.Dark }, SettingsJsonContext.Default.SettingsFileModel));
        }

        Assert.Equal(AppTheme.Light, JsonFile.Read(path, SettingsJsonContext.Default.SettingsFileModel)!.Theme);
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public void Settings_WrittenByVersion151_StillLoad()
    {
        // config.json exactly as v1.5.1 wrote it (reflection serializer, PascalCase, enum names).
        var path = Path.Combine(_directory, "config.json");
        File.WriteAllText(path, """
            {
              "AcceptedTermsVersion": "1.1",
              "AutoCheckForUpdates": false,
              "Theme": "Dark",
              "Language": "vi-VN",
              "FontSize": "Large",
              "CollapseSidebarByDefault": true,
              "ShowAdminRestartBadges": false,
              "ConfirmCaution": false,
              "AutoCreateRestorePoint": false,
              "AutoScrollConsole": true,
              "WarnOnCancel": false,
              "LogLevel": "Debug",
              "FavoriteCommandIds": [ "net.flushdns", "sfc.scannow" ],
              "RecentCommandIds": [ "info.systeminfo" ],
              "WindowWidth": null,
              "WindowHeight": null,
              "WindowLeft": 120.5,
              "WindowTop": 64,
              "WindowMaximized": false
            }
            """);

        var settings = new JsonSettingsService(path);

        Assert.Equal("1.1", settings.AcceptedTermsVersion); // no second Legal Gate after the update
        Assert.False(settings.AutoCheckForUpdates);
        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Equal("vi-VN", settings.Language);
        Assert.Equal(FontScale.Large, settings.FontSize);
        Assert.True(settings.CollapseSidebarByDefault);
        Assert.False(settings.ShowAdminRestartBadges);
        Assert.False(settings.ConfirmCaution);
        Assert.False(settings.AutoCreateRestorePoint);
        Assert.False(settings.WarnOnCancel);
        Assert.Equal(LogLevel.Debug, settings.LogLevel);
        Assert.Equal(["net.flushdns", "sfc.scannow"], settings.FavoriteCommandIds);
        Assert.Equal(["info.systeminfo"], settings.RecentCommandIds);
        Assert.Null(settings.WindowWidth);
        Assert.Equal(120.5, settings.WindowLeft);
        Assert.Equal(64, settings.WindowTop);
    }

    [Fact]
    public void Settings_HandEditedValues_AreRepaired()
    {
        var path = Path.Combine(_directory, "config.json");
        File.WriteAllText(path, """
            { "Theme": 42, "Language": null, "FavoriteCommandIds": null, "RecentCommandIds": [ "a", " ", "a", "b" ] }
            """);

        var settings = new JsonSettingsService(path);

        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Equal(string.Empty, settings.Language);
        Assert.Empty(settings.FavoriteCommandIds);
        Assert.Equal(["a", "b"], settings.RecentCommandIds);
    }
}
