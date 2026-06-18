using CommandForge.Application.Logging;
using CommandForge.Application.Settings;
using CommandForge.Infrastructure;

namespace CommandForge.Tests;

/// <summary>Verifies settings persist to / load from config.json, including defaults for missing keys.</summary>
public sealed class SettingsRoundTripTests
{
    [Fact]
    public void Settings_RoundTrip_JsonSerialization()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-settings-{Guid.NewGuid():N}.json");
        try
        {
            var writer = new JsonSettingsService(path)
            {
                AcceptedTermsVersion = "1.0",
                AutoCheckForUpdates = false,
                Theme = AppTheme.Dark,
                Language = "vi",
                FontSize = FontScale.Large,
                CollapseSidebarByDefault = true,
                ShowAdminRestartBadges = false,
                ConfirmCaution = false,
                AutoCreateRestorePoint = false,
                AutoScrollConsole = false,
                WarnOnCancel = false,
                LogLevel = LogLevel.Verbose,
                FavoriteCommandIds = ["info.systeminfo", "net.flushdns"],
                RecentCommandIds = ["dism.restorehealth"],
                WindowWidth = 1280,
                WindowHeight = 800,
                WindowLeft = 120,
                WindowTop = 64,
                WindowMaximized = true,
            };
            writer.Save();

            var reader = new JsonSettingsService(path);
            Assert.Equal("1.0", reader.AcceptedTermsVersion);
            Assert.False(reader.AutoCheckForUpdates);
            Assert.Equal(AppTheme.Dark, reader.Theme);
            Assert.Equal("vi", reader.Language);
            Assert.Equal(FontScale.Large, reader.FontSize);
            Assert.True(reader.CollapseSidebarByDefault);
            Assert.False(reader.ShowAdminRestartBadges);
            Assert.False(reader.ConfirmCaution);
            Assert.False(reader.AutoCreateRestorePoint);
            Assert.False(reader.AutoScrollConsole);
            Assert.False(reader.WarnOnCancel);
            Assert.Equal(LogLevel.Verbose, reader.LogLevel);
            Assert.Equal(["info.systeminfo", "net.flushdns"], reader.FavoriteCommandIds);
            Assert.Equal(["dism.restorehealth"], reader.RecentCommandIds);
            Assert.Equal(1280, reader.WindowWidth);
            Assert.Equal(800, reader.WindowHeight);
            Assert.Equal(120, reader.WindowLeft);
            Assert.Equal(64, reader.WindowTop);
            Assert.True(reader.WindowMaximized);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Settings_MissingKeys_UseDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-settings-{Guid.NewGuid():N}.json");
        try
        {
            // Only one key present — everything else must fall back to the documented defaults.
            File.WriteAllText(path, "{ \"AcceptedTermsVersion\": \"0.9\" }");
            var settings = new JsonSettingsService(path);

            Assert.Equal("0.9", settings.AcceptedTermsVersion);
            Assert.True(settings.AutoCheckForUpdates);
            Assert.Equal(AppTheme.System, settings.Theme);
            Assert.Equal("", settings.Language);
            Assert.Equal(FontScale.Medium, settings.FontSize);
            Assert.False(settings.CollapseSidebarByDefault);
            Assert.True(settings.ShowAdminRestartBadges);
            Assert.True(settings.ConfirmCaution);
            Assert.True(settings.AutoCreateRestorePoint);
            Assert.True(settings.AutoScrollConsole);
            Assert.True(settings.WarnOnCancel);
            Assert.Equal(LogLevel.Information, settings.LogLevel);
            Assert.Empty(settings.FavoriteCommandIds);
            Assert.Empty(settings.RecentCommandIds);
            Assert.Null(settings.WindowWidth);
            Assert.Null(settings.WindowHeight);
            Assert.Null(settings.WindowLeft);
            Assert.Null(settings.WindowTop);
            Assert.False(settings.WindowMaximized);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
