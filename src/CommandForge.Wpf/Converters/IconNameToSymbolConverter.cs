using System.Globalization;
using System.Windows.Data;

namespace CommandForge.Desktop.Converters;

/// <summary>Converts a catalog icon name to a Fluent <see cref="Wpf.Ui.Controls.SymbolRegular"/> (see <see cref="IconMap"/>).</summary>
public sealed class IconNameToSymbolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => IconMap.Resolve(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
