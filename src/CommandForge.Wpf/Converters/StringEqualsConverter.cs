using System.Globalization;
using System.Windows.Data;

namespace CommandForge.Wpf.Converters;

/// <summary>
/// Returns <see langword="true"/> when the bound value equals the converter parameter (ordinal
/// string compare). Used for radio-style menu checkmarks (e.g. the Language submenu).
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString() ?? string.Empty, parameter?.ToString() ?? string.Empty, StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
