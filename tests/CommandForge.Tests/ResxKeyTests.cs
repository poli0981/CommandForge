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
}
