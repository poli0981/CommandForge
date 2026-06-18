using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CommandForge.Wpf.Converters;

/// <summary>Returns <see cref="Visibility.Visible"/> only when every bound boolean is <see langword="true"/>.</summary>
public sealed class MultiBoolToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        => values.All(v => v is true) ? Visibility.Visible : Visibility.Collapsed;

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
