using CommandForge.Application.Ports;

namespace CommandForge.Application.Settings;

/// <summary>
/// Pure mapping between the live <see cref="ISettingsService"/> and the portable subset
/// (<see cref="PortableSettings"/>) used for import/export and profiles.
/// </summary>
public static class SettingsTransfer
{
    /// <summary>Reads the current portable settings from the live settings service.</summary>
    public static PortableSettings Capture(ISettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new PortableSettings
        {
            Theme = settings.Theme,
            Language = settings.Language,
            FontSize = settings.FontSize,
            CollapseSidebarByDefault = settings.CollapseSidebarByDefault,
            ShowAdminRestartBadges = settings.ShowAdminRestartBadges,
            ConfirmCaution = settings.ConfirmCaution,
            AutoCreateRestorePoint = settings.AutoCreateRestorePoint,
            AutoScrollConsole = settings.AutoScrollConsole,
            WarnOnCancel = settings.WarnOnCancel,
            AutoCheckForUpdates = settings.AutoCheckForUpdates,
            LogLevel = settings.LogLevel,
            FavoriteCommandIds = settings.FavoriteCommandIds,
        };
    }

    /// <summary>
    /// Writes the portable settings into the live settings service (does NOT call Save, and does
    /// not apply live UI effects — callers persist and re-apply). Used in non-UI contexts and tests.
    /// </summary>
    public static void Apply(PortableSettings portable, ISettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(portable);
        ArgumentNullException.ThrowIfNull(settings);

        settings.Theme = portable.Theme;
        settings.Language = portable.Language;
        settings.FontSize = portable.FontSize;
        settings.CollapseSidebarByDefault = portable.CollapseSidebarByDefault;
        settings.ShowAdminRestartBadges = portable.ShowAdminRestartBadges;
        settings.ConfirmCaution = portable.ConfirmCaution;
        settings.AutoCreateRestorePoint = portable.AutoCreateRestorePoint;
        settings.AutoScrollConsole = portable.AutoScrollConsole;
        settings.WarnOnCancel = portable.WarnOnCancel;
        settings.AutoCheckForUpdates = portable.AutoCheckForUpdates;
        settings.LogLevel = portable.LogLevel;
        settings.FavoriteCommandIds = portable.FavoriteCommandIds;
    }
}
