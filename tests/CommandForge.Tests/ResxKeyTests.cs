using System.Globalization;
using System.Resources;
using CommandForge.Infrastructure.Catalog;
using CommandForge.Wpf.Resources;

namespace CommandForge.Tests;

/// <summary>
/// Cross-checks that every catalog title/description resource key exists in both the neutral
/// (EN) resources and the Vietnamese satellite (no parent fallback).
/// </summary>
public sealed class ResxKeyTests
{
    private static readonly CatalogLoadResult Catalog = CatalogLoader.LoadEmbedded();

    [Fact]
    public void Catalog_AllKeys_ExistInResx_EN_and_VI()
    {
        var manager = new ResourceManager("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        var vietnamese = manager.GetResourceSet(new CultureInfo("vi"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(english);
        Assert.NotNull(vietnamese);

        var keys = new List<string>();
        keys.AddRange(Catalog.Categories.Select(c => c.TitleKey));
        keys.AddRange(Catalog.Commands.Select(c => c.TitleKey));
        keys.AddRange(Catalog.Commands.Select(c => c.DescriptionKey));

        foreach (var key in keys)
        {
            Assert.True(english!.GetString(key) is not null, $"Missing EN resource for key '{key}'.");
            Assert.True(vietnamese!.GetString(key) is not null, $"Missing VI resource for key '{key}'.");
        }
    }

    [Fact]
    public void SettingsUiKeys_ExistInResx_EN_and_VI()
    {
        var manager = new ResourceManager("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        var vietnamese = manager.GetResourceSet(new CultureInfo("vi"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(english);
        Assert.NotNull(vietnamese);

        string[] keys =
        [
            "Menu_OpenSettings", "LanguageOption_System",
            "Settings_Appearance", "Settings_Behavior", "Settings_Updates", "Settings_About",
            "Settings_Theme", "Settings_ThemeSystem", "Settings_ThemeLight", "Settings_ThemeDark",
            "Settings_Language", "Settings_FontSize", "Settings_FontSmall", "Settings_FontMedium", "Settings_FontLarge",
            "Settings_CollapseSidebar", "Settings_ShowBadges",
            "Settings_ConfirmCaution", "Settings_ConfirmDangerous", "Settings_DangerousLockedTooltip",
            "Settings_AutoRestorePoint", "Settings_AutoScroll", "Settings_WarnOnCancel",
            "Settings_AutoCheckUpdates", "Settings_CurrentVersion", "Settings_LastChecked", "Settings_CheckNow",
            "Settings_NoDataCollected", "Settings_License", "Settings_GitHub",
        ];

        foreach (var key in keys)
        {
            Assert.True(english!.GetString(key) is not null, $"Missing EN resource for key '{key}'.");
            Assert.True(vietnamese!.GetString(key) is not null, $"Missing VI resource for key '{key}'.");
        }
    }

    [Fact]
    public void DiagnosticsUiKeys_ExistInResx_EN_and_VI()
    {
        var manager = new ResourceManager("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        var vietnamese = manager.GetResourceSet(new CultureInfo("vi"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(english);
        Assert.NotNull(vietnamese);

        string[] keys =
        [
            "Menu_LogViewer", "Menu_ExportLogs", "Menu_DebugPanel", "Menu_ReportBug",
            "LogViewer_Title", "LogViewer_Level", "LogViewer_FilterHint", "LogViewer_AutoScroll",
            "LogViewer_OpenFolder", "LogViewer_Export", "LogViewer_Clear",
            "LogLevel_All", "LogLevel_Info", "LogLevel_Warning", "LogLevel_Error", "LogLevel_Debug", "Log_ExportSuccess",
            "Debug_TabVerbose", "Debug_TabCatalog", "Debug_TabSystemInfo", "Debug_CategoryCount", "Debug_CommandCount",
            "Debug_ValidationErrors", "Debug_NoValidationErrors", "Debug_CopySystemInfo", "Debug_Refresh",
            "Error_Title", "Error_Message", "Error_Code", "Error_CopyDetails", "Error_ReportBug", "Error_Close",
            "ReportBug_Title", "ReportBug_Environment", "ReportBug_ExportLog", "ReportBug_OpenIssue",
            "ReportBug_Close", "ReportBug_DescriptionHint", "ReportBug_ReviewReminder",
            "Settings_Logs", "Settings_LogLevel", "Settings_LogsSize", "Settings_OpenLogFolder", "Settings_ExportLogs",
            "Settings_ClearOldLogs", "Settings_ClearLogsConfirm", "Settings_Advanced", "Settings_RunMode",
            "Settings_RunModeInstalled", "Settings_RunModePortable", "Settings_ConfigPath", "Settings_OpenConfig",
            "Settings_OpenDebugPanel", "Settings_ResetDefaults", "Settings_ResetConfirm",
        ];

        foreach (var key in keys)
        {
            Assert.True(english!.GetString(key) is not null, $"Missing EN resource for key '{key}'.");
            Assert.True(vietnamese!.GetString(key) is not null, $"Missing VI resource for key '{key}'.");
        }
    }

    [Fact]
    public void HomeUiKeys_ExistInResx_EN_and_VI()
    {
        var manager = new ResourceManager("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        var vietnamese = manager.GetResourceSet(new CultureInfo("vi"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(english);
        Assert.NotNull(vietnamese);

        string[] keys =
        [
            "SidebarHome", "SidebarFavorites", "Favorite_Toggle",
            "Home_SystemStatus", "Home_StatusOs", "Home_StatusMemory", "Home_StatusDisk", "Home_StatusUptime",
            "Home_OsFormat", "Home_StorageFormat", "Home_GigabytesFormat", "Home_DiskFormat",
            "Home_UptimeWithDays", "Home_UptimeNoDays",
            "Home_Recent", "Home_NoRecent", "Home_NoFavorites",
        ];

        foreach (var key in keys)
        {
            Assert.True(english!.GetString(key) is not null, $"Missing EN resource for key '{key}'.");
            Assert.True(vietnamese!.GetString(key) is not null, $"Missing VI resource for key '{key}'.");
        }
    }

    [Fact]
    public void LegalUiKeys_ExistInResx_EN_and_VI()
    {
        var manager = new ResourceManager("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        var vietnamese = manager.GetResourceSet(new CultureInfo("vi"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(english);
        Assert.NotNull(vietnamese);

        string[] keys =
        [
            "LegalGateTitle", "LegalGateIntro", "LegalGateAgree", "LegalGateContinue", "LegalGateExit",
            "LegalGateReadOnGitHub", "TabEula", "TabGpl", "TabDisclaimer", "TabPrivacy",
            "Legal_EulaSummary", "Legal_GplSummary", "Legal_DisclaimerSummary", "Legal_PrivacySummary",
            "LegalViewer_Title",
        ];

        foreach (var key in keys)
        {
            Assert.True(english!.GetString(key) is not null, $"Missing EN resource for key '{key}'.");
            Assert.True(vietnamese!.GetString(key) is not null, $"Missing VI resource for key '{key}'.");
        }
    }

    [Fact]
    public void MenuUiKeys_ExistInResx_EN_and_VI()
    {
        var manager = new ResourceManager("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        var vietnamese = manager.GetResourceSet(new CultureInfo("vi"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(english);
        Assert.NotNull(vietnamese);

        string[] keys =
        [
            "MenuFile", "MenuView", "MenuTools", "MenuAbout", "MenuHelp", "MenuExit", "MenuToggleSidebar",
            "Menu_OpenConfigFolder", "Menu_OpenLogFolder", "Menu_FullScreen", "Menu_About", "Menu_Wiki",
            "Menu_OpenSettings", "Menu_LogViewer", "Menu_ExportLogs", "Menu_DebugPanel",
            "Menu_CheckForUpdates", "Menu_ReportBug", "Palette_Placeholder",
            "Menu_Wiki", "Menu_About", "Menu_OpenConfigFolder", "Menu_OpenLogFolder",
            "Menu_FullScreen", "Menu_Terms",
        ];

        foreach (var key in keys)
        {
            Assert.True(english!.GetString(key) is not null, $"Missing EN resource for key '{key}'.");
            Assert.True(vietnamese!.GetString(key) is not null, $"Missing VI resource for key '{key}'.");
        }
    }
}
