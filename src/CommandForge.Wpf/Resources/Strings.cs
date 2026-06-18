using System.Globalization;
using System.Resources;

namespace CommandForge.Wpf.Resources;

/// <summary>
/// Strongly-typed access to localized UI strings (EN neutral + VI satellite). Most XAML binds via
/// the <c>{res:Loc Key}</c> markup extension (live culture switching); these typed members back the
/// few <c>x:Static</c> bindings (the Legal Gate) and code-behind lookups via <see cref="Get"/>.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager Manager =
        new("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);

    /// <summary>Resolves an arbitrary resource key for the current UI culture.</summary>
    public static string Get(string key) =>
        Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

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
}
