using System.Globalization;
using System.Resources;

namespace CommandForge.Wpf.Resources;

/// <summary>
/// Strongly-typed access to localized UI strings (EN neutral + VI satellite).
/// Phase 0 wires the i18n pipeline; full runtime culture switching comes later.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager Manager =
        new("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);

    /// <summary>Resolves an arbitrary resource key for the current UI culture.</summary>
    public static string Get(string key) =>
        Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string AppTitle => Get(nameof(AppTitle));

    public static string LegalGateTitle => Get(nameof(LegalGateTitle));
    public static string LegalGateIntro => Get(nameof(LegalGateIntro));
    public static string LegalGateReadOnGitHub => Get(nameof(LegalGateReadOnGitHub));
    public static string LegalGateAgree => Get(nameof(LegalGateAgree));
    public static string LegalGateContinue => Get(nameof(LegalGateContinue));
    public static string LegalGateExit => Get(nameof(LegalGateExit));
    public static string TabEula => Get(nameof(TabEula));
    public static string TabGpl => Get(nameof(TabGpl));
    public static string TabDisclaimer => Get(nameof(TabDisclaimer));
    public static string TabPrivacy => Get(nameof(TabPrivacy));

    public static string MenuFile => Get(nameof(MenuFile));
    public static string MenuView => Get(nameof(MenuView));
    public static string MenuTools => Get(nameof(MenuTools));
    public static string MenuAbout => Get(nameof(MenuAbout));
    public static string MenuHelp => Get(nameof(MenuHelp));
    public static string MenuExit => Get(nameof(MenuExit));
    public static string MenuToggleSidebar => Get(nameof(MenuToggleSidebar));

    public static string SidebarHome => Get(nameof(SidebarHome));
    public static string SidebarFavorites => Get(nameof(SidebarFavorites));
    public static string SidebarSettings => Get(nameof(SidebarSettings));

    public static string PaneCommands => Get(nameof(PaneCommands));
    public static string PaneDetails => Get(nameof(PaneDetails));
    public static string PaneConsole => Get(nameof(PaneConsole));
    public static string ComingSoon => Get(nameof(ComingSoon));
}
