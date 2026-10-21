using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommandForge.Desktop.Converters;

/// <summary>Visible when a count is zero (an empty-list placeholder), collapsed otherwise.</summary>
public sealed class ZeroToVisibleConverter : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
