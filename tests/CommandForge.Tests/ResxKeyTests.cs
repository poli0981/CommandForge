using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using CommandForge.Desktop.Resources;
using CommandForge.Infrastructure.Catalog;

namespace CommandForge.Tests;

/// <summary>
/// Cross-checks that every catalog title/description and UI resource key exists in the neutral
/// (EN) resources and in every satellite culture (no parent fallback), so a missing translation
/// fails the build rather than silently falling back to English.
/// </summary>
public sealed partial class ResxKeyTests
{
    private static readonly CatalogLoadResult Catalog = CatalogLoader.LoadEmbedded();

    /// <summary>The non-neutral UI cultures that must have a full satellite resource set (no fallback).</summary>
    private static readonly string[] SatelliteCultures = ["vi", "ja", "zh-Hans", "es"];

    /// <summary>Asserts every key resolves in EN (neutral) and in every satellite culture without fallback.</summary>
    private static void AssertKeysExist(IEnumerable<string> keys)
    {
        var manager = new ResourceManager("CommandForge.Desktop.Resources.Strings", typeof(Strings).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        Assert.NotNull(english);

        var satellites = SatelliteCultures.ToDictionary(
            culture => culture,
            culture => manager.GetResourceSet(new CultureInfo(culture), createIfNotExists: true, tryParents: false));

        foreach (var (culture, set) in satellites)
        {
            Assert.True(set is not null, $"Missing satellite resource set for culture '{culture}'.");
        }

        foreach (var key in keys)
        {
            Assert.True(english.GetString(key) is not null, $"Missing EN resource for key '{key}'.");
            foreach (var (culture, set) in satellites)
            {
                Assert.True(set!.GetString(key) is not null, $"Missing {culture} resource for key '{key}'.");
            }
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
    public void ShellUiKeys_ExistInResx_AllCultures()
    {
        string[] keys =
        [
            "MenuExit", "MenuToggleSidebar", "Menu_More", "Menu_UserGuide", "Nav_Catalog", "TitleBar_Search",
            "Menu_OpenConfigFolder", "Menu_OpenLogFolder", "Menu_About", "Menu_Terms",
            "Menu_OpenSettings", "Menu_LogViewer", "Menu_ExportLogs", "Menu_DebugPanel",
            "Menu_CheckForUpdates", "Menu_ReportBug", "Palette_Placeholder",
            "Menu_KeyboardShortcuts", "Menu_PortableInfo", "Menu_RestorePoint",
            "Admin_SessionActive", "Admin_SessionTooltip", "Admin_EndSession", "Admin_SessionEnded",
            "Run_Finished", "Run_FinishedFailed", "Snack_Copied", "Snack_CopyFailed",
            "Dialog_Yes", "Dialog_No", "Dialog_OK",
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

    [Fact]
    public void EveryKeyTheUiUses_ExistsInResx_AllCultures()
    {
        // Scans the presentation sources, so a new {res:Loc ...} or Strings.Get("...") can't ship
        // without its translations.
        var root = FindRepositoryRoot();
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "src", "CommandForge.Wpf"), "*.*", SearchOption.AllDirectories))
        {
            if (!path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            if (path.Split(separators).Any(part => part is "bin" or "obj"))
            {
                continue;
            }

            foreach (var line in File.ReadLines(path))
            {
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue; // documentation examples are not real keys
                }

                foreach (Match match in UiKeyPattern().Matches(line))
                {
                    keys.Add(match.Groups["key"].Value);
                }
            }
        }

        Assert.True(keys.Count > 100, $"Expected the scan to find the UI's keys, found {keys.Count}.");
        AssertKeysExist(keys);
    }

    [Fact]
    public void DynamicKeys_ExistInResx_AllCultures()
    {
        // Keys built at runtime from enum names: Danger_{level}, Estimate_{duration}.
        var keys = Enum.GetNames<CommandForge.Domain.DangerLevel>().Select(level => "Danger_" + level)
            .Concat(Enum.GetNames<CommandForge.Domain.EstimatedDuration>().Select(duration => "Estimate_" + duration));

        AssertKeysExist(keys);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CommandForge.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find the repository root (CommandForge.slnx) above " + AppContext.BaseDirectory);
    }

    // {res:Loc Key} in XAML, Strings.Get("Key") / Localized("Key") in code.
    [GeneratedRegex(@"(?:res:Loc\s+|Strings\.Get\(""|Localized\("")(?<key>[A-Za-z][A-Za-z0-9_]*)(?:""|[\s}])", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 2000)]
    private static partial Regex UiKeyPattern();
}
