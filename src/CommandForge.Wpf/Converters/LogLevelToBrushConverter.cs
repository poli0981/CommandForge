using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CommandForge.Application.Logging;

namespace CommandForge.Wpf.Converters;

/// <summary>
/// Maps a <see cref="LogLevel"/> to a row foreground: Warning=amber, Error/Fatal=red, Debug/Verbose=muted
/// gray (all readable in both themes); Information/default returns <see cref="DependencyProperty.UnsetValue"/>
/// so the row inherits the themed body foreground. Color is never the only cue — a level tag is shown too.
/// </summary>
public sealed class LogLevelToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Warning = Frozen(0xE0, 0xA0, 0x30);
    private static readonly SolidColorBrush Error = Frozen(0xE0, 0x5B, 0x5B);
    private static readonly SolidColorBrush Muted = Frozen(0x9A, 0x9A, 0x9A);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            LogLevel.Warning => Warning,
            LogLevel.Error or LogLevel.Fatal => Error,
            LogLevel.Debug or LogLevel.Verbose => Muted,
            _ => DependencyProperty.UnsetValue, // Information → inherit themed foreground
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static SolidColorBrush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
