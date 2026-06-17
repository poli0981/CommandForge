using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CommandForge.Domain;

namespace CommandForge.Wpf.Converters;

/// <summary>Maps a <see cref="DangerLevel"/> to its indicator brush (green / amber / red).</summary>
public sealed class DangerLevelToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            DangerLevel.Safe => Brushes.MediumSeaGreen,
            DangerLevel.Caution => Brushes.Orange,
            DangerLevel.Dangerous => Brushes.IndianRed,
            _ => Brushes.Gray,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
