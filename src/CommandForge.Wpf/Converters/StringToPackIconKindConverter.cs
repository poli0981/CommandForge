using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace CommandForge.Wpf.Converters;

/// <summary>
/// Converts a catalog icon name to a <see cref="PackIconKind"/>, falling back to a neutral
/// icon when the name is missing or unknown (so a bad catalog icon never crashes the UI).
/// </summary>
public sealed class StringToPackIconKindConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string name && Enum.TryParse<PackIconKind>(name, ignoreCase: true, out var kind)
            ? kind
            : PackIconKind.ChevronRight;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
