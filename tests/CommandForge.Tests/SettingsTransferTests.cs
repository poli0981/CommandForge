using CommandForge.Application.Logging;
using CommandForge.Application.Settings;
using CommandForge.Infrastructure;

namespace CommandForge.Tests;

/// <summary>Verifies the portable-settings capture/apply mapping covers every field (round-trip).</summary>
public sealed class SettingsTransferTests
{
    [Fact]
    public void Capture_Then_Apply_RoundTripsAllFields()
    {
        var srcPath = Path.Combine(Path.GetTempPath(), $"cf-src-{Guid.NewGuid():N}.json");
        var dstPath = Path.Combine(Path.GetTempPath(), $"cf-dst-{Guid.NewGuid():N}.json");
        try
        {
            var src = new JsonSettingsService(srcPath)
            {
                Theme = AppTheme.Dark,
                Language = "vi",
                FontSize = FontScale.Large,
                CollapseSidebarByDefault = true,
                ShowAdminRestartBadges = false,
                ConfirmCaution = false,
                AutoCreateRestorePoint = false,
                AutoScrollConsole = false,
                WarnOnCancel = false,
                AutoCheckForUpdates = false,
                LogLevel = LogLevel.Verbose,
                FavoriteCommandIds = ["info.systeminfo", "net.flushdns"],
            };

            var captured = SettingsTransfer.Capture(src);

            Assert.Equal(AppTheme.Dark, captured.Theme);
            Assert.Equal("vi", captured.Language);
            Assert.Equal(FontScale.Large, captured.FontSize);
            Assert.Equal(["info.systeminfo", "net.flushdns"], captured.FavoriteCommandIds);

            var dst = new JsonSettingsService(dstPath);
            SettingsTransfer.Apply(captured, dst);

            Assert.Equal(AppTheme.Dark, dst.Theme);
            Assert.Equal("vi", dst.Language);
            Assert.Equal(FontScale.Large, dst.FontSize);
            Assert.True(dst.CollapseSidebarByDefault);
            Assert.False(dst.ShowAdminRestartBadges);
            Assert.False(dst.ConfirmCaution);
            Assert.False(dst.AutoCreateRestorePoint);
            Assert.False(dst.AutoScrollConsole);
            Assert.False(dst.WarnOnCancel);
            Assert.False(dst.AutoCheckForUpdates);
            Assert.Equal(LogLevel.Verbose, dst.LogLevel);
            Assert.Equal(["info.systeminfo", "net.flushdns"], dst.FavoriteCommandIds);
        }
        finally
        {
            File.Delete(srcPath);
            File.Delete(dstPath);
        }
    }
}
