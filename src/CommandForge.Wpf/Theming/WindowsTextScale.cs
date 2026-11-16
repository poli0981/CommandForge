using System.IO;
using System.Security;
using Microsoft.Win32;

namespace CommandForge.Desktop.Theming;

/// <summary>
/// Windows' "Text size" accessibility setting (Settings › Accessibility › Text size, 100–225 %),
/// which WPF does not apply by itself. Only read, never changed.
/// </summary>
public static class WindowsTextScale
{
    /// <summary>Windows' default: no enlargement.</summary>
    public const double Minimum = 1.0;

    /// <summary>The largest text size Windows offers (225 %).</summary>
    public const double Maximum = 2.25;

    /// <summary>The current factor, or <see cref="Minimum"/> when the setting is unset or unreadable.</summary>
    public static double Read()
    {
#if DEBUG
        // Lets a developer check layouts at large sizes without changing the Windows setting.
        if (double.TryParse(
            Environment.GetEnvironmentVariable("COMMANDFORGE_TEXT_SCALE"),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var developerFactor))
        {
            return Clamp(developerFactor);
        }
#endif
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Accessibility");
            return FromRegistryValue(key?.GetValue("TextScaleFactor"));
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            return Minimum;
        }
    }

    /// <summary>Pure: the stored DWORD percentage (100–225) as a factor, clamped; anything else is 1.0.</summary>
    public static double FromRegistryValue(object? value) => value is int percent ? Clamp(percent / 100d) : Minimum;

    /// <summary>Pure: keeps a factor within what Windows offers.</summary>
    public static double Clamp(double factor) => double.IsFinite(factor) ? Math.Clamp(factor, Minimum, Maximum) : Minimum;
}
