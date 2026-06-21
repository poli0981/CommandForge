using System.Globalization;
using System.Resources;
using CommandForge.Infrastructure.Catalog;
using CommandForge.Wpf.Resources;

namespace CommandForge.Tests;

/// <summary>
/// Cross-checks that every catalog title/description and UI resource key exists in the neutral
/// (EN) resources and in both the Vietnamese and Japanese satellites (no parent fallback), so a
/// missing translation fails the build rather than silently falling back to English.
/// </summary>
public sealed class ResxKeyTests
{
    private static readonly CatalogLoadResult Catalog = CatalogLoader.LoadEmbedded();

    /// <summary>Asserts every key resolves in EN (neutral) and in the VI and JA satellites without fallback.</summary>
    private static void AssertKeysExist(IEnumerable<string> keys)
    {
        var manager = new ResourceManager("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        var vietnamese = manager.GetResourceSet(new CultureInfo("vi"), createIfNotExists: true, tryParents: false);
        var japanese = manager.GetResourceSet(new CultureInfo("ja"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(english);
        Assert.NotNull(vietnamese);
        Assert.NotNull(japanese);

        foreach (var key in keys)
        {
            Assert.True(english!.GetString(key) is not null, $"Missing EN resource for key '{key}'.");
            Assert.True(vietnamese!.GetString(key) is not null, $"Missing VI resource for key '{key}'.");
            Assert.True(japanese!.GetString(key) is not null, $"Missing JA resource for key '{key}'.");
        }
    }

    [Fact]
    public void Catalog_AllKeys_ExistInResx_AllCultures()
    {
        var keys = new List<string>();
        keys.AddRange(Catalog.Categories.Select(c => c.TitleKey));
        keys.AddRange(Catalog.Commands.Select(c => c.TitleKey));
        keys.AddRange(Catalog.Commands.Select(c => c.DescriptionKey));

        AssertKeysExist(keys);
    }

    [Fact]
    public void SettingsUiKeys_ExistInResx_AllCultures()
    {
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
            "Settings_Profiles", "Settings_ExportImport", "Settings_ExportImportHint",
            "Settings_ExportSettings", "Settings_ImportSettings", "Settings_SavedProfiles", "Settings_ProfilesHint",
            "Settings_ApplyProfile", "Settings_DeleteProfile", "Settings_SaveCurrentAsProfile",
            "Settings_ProfileName", "Settings_SaveProfile", "Settings_ExportSuccess", "Settings_ImportConfirm",
            "Settings_ImportInvalid", "Settings_ImportSuccess", "Settings_ProfileApplied", "Settings_DeleteProfileConfirm",
        ];

        AssertKeysExist(keys);
    }

    [Fact]
    public void DiagnosticsUiKeys_ExistInResx_AllCultures()
    {
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

        AssertKeysExist(keys);
    }

    [Fact]
    public void HomeUiKeys_ExistInResx_AllCultures()
    {
        string[] keys =
        [
            "SidebarHome", "SidebarFavorites", "Favorite_Toggle",
            "Home_SystemStatus", "Home_StatusOs", "Home_StatusMemory", "Home_StatusDisk", "Home_StatusUptime",
            "Home_OsFormat", "Home_StorageFormat", "Home_GigabytesFormat", "Home_DiskFormat",
            "Home_UptimeWithDays", "Home_UptimeNoDays",
            "Home_Recent", "Home_NoRecent", "Home_NoFavorites",
        ];

        AssertKeysExist(keys);
    }

    [Fact]
    public void LegalUiKeys_ExistInResx_AllCultures()
    {
        string[] keys =
        [
            "LegalGateTitle", "LegalGateIntro", "LegalGateAgree", "LegalGateContinue", "LegalGateExit",
            "LegalGateReadOnGitHub", "TabEula", "TabGpl", "TabDisclaimer", "TabPrivacy",
            "Legal_EulaSummary", "Legal_GplSummary", "Legal_DisclaimerSummary", "Legal_PrivacySummary",
            "LegalViewer_Title",
        ];

        AssertKeysExist(keys);
    }

    [Fact]
    public void MenuUiKeys_ExistInResx_AllCultures()
    {
        string[] keys =
        [
            "MenuFile", "MenuView", "MenuTools", "MenuAbout", "MenuHelp", "MenuExit", "MenuToggleSidebar",
            "Menu_OpenConfigFolder", "Menu_OpenLogFolder", "Menu_FullScreen", "Menu_About", "Menu_Wiki",
            "Menu_OpenSettings", "Menu_LogViewer", "Menu_ExportLogs", "Menu_DebugPanel",
            "Menu_CheckForUpdates", "Menu_ReportBug", "Palette_Placeholder",
            "Menu_Wiki", "Menu_About", "Menu_OpenConfigFolder", "Menu_OpenLogFolder",
            "Menu_FullScreen", "Menu_Terms",
            "Menu_KeyboardShortcuts", "Menu_PortableInfo", "Menu_RestorePoint",
        ];

        AssertKeysExist(keys);
    }

    [Fact]
    public void DialogUiKeys_ExistInResx_AllCultures()
    {
        string[] keys =
        [
            "Shortcuts_Palette", "Shortcuts_ListNav", "Shortcuts_OpenSelected", "Shortcuts_CloseDialog",
            "PortableInfo_ConfigFolder", "PortableInfo_LogFolder",
            "Settings_RunMode", "Settings_RunModeInstalled", "Settings_RunModePortable",
            "Settings_CurrentVersion", "Settings_NoDataCollected", "LanguageOption_System",
        ];

        AssertKeysExist(keys);
    }

    [Fact]
    public void RecipesUiKeys_ExistInResx_AllCultures()
    {
        string[] keys =
        [
            "SidebarRecipes", "Recipes_Saved", "Recipes_New", "Recipe_Delete", "Recipes_NameHint",
            "Recipes_NoSteps", "Recipes_AddHint", "Recipes_AddStep", "Recipes_Save", "Recipes_Run",
            "Recipes_MoveUp", "Recipes_MoveDown", "Recipes_RemoveStep",
            "Recipe_StepHeader", "Recipe_StoppedOnError", "Recipe_StoppedRestart", "Recipe_Cancelled",
            "Recipe_ContainsDangerous", "Recipe_ConfirmTitle", "Recipe_DeleteConfirm",
        ];

        AssertKeysExist(keys);
    }

    [Fact]
    public void UserCommandsUiKeys_ExistInResx_AllCultures()
    {
        string[] keys =
        [
            "SidebarUserCommands", "UserCmd_Warning", "UserCmd_Saved", "UserCmd_New", "UserCmd_Delete",
            "UserCmd_NameHint", "UserCmd_ExecutableHint", "UserCmd_ArgumentsHint", "UserCmd_Save", "UserCmd_Run",
            "UserCmd_NoCommands", "UserCmd_RunTitle", "UserCmd_RunConfirm", "UserCmd_DeleteConfirm",
        ];

        AssertKeysExist(keys);
    }

    [Fact]
    public void RegistryAndUndoUiKeys_ExistInResx_AllCultures()
    {
        string[] keys =
        [
            "History_Undo", "Registry_ChangesHeader", "Registry_ChangeFormat", "Registry_NotSet", "Registry_NoChanges",
        ];

        AssertKeysExist(keys);
    }

    [Fact]
    public void DashboardAndSearchUiKeys_ExistInResx_AllCultures()
    {
        string[] keys =
        [
            "Search_Suggestion", "Home_Suggestions", "Maint_LowDisk", "Maint_HighUptime", "Maint_Open",
        ];

        AssertKeysExist(keys);
    }
}
