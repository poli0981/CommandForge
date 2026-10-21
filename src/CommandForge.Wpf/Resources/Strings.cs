using System.Globalization;
using System.Resources;

namespace CommandForge.Desktop.Resources;

/// <summary>
/// Resolves localized UI strings (EN neutral + VI satellite). XAML binds via the
/// <c>{res:Loc Key}</c> markup extension (live culture switching); code-behind uses <see cref="Get"/>.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager Manager =
        new("CommandForge.Desktop.Resources.Strings", typeof(Strings).Assembly);

    /// <summary>Resolves an arbitrary resource key for the current UI culture.</summary>
    public static string Get(string key) =>
        Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
