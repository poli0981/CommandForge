using System.Globalization;
using System.Windows.Data;
using CommandForge.Domain;
using CommandForge.Wpf.Resources;

namespace CommandForge.Wpf.Converters;

/// <summary>Maps a <see cref="DangerLevel"/> to its localized label (Danger_Safe / _Caution / _Dangerous).</summary>
public sealed class DangerLevelToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DangerLevel level ? Strings.Get($"Danger_{level}") : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
