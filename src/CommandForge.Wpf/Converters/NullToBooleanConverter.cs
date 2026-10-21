using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommandForge.Desktop.Converters;

/// <summary>True when the value is set (not null, and not an empty string), e.g. to open an info bar.</summary>
public sealed class NullToBooleanConverter : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string text ? text.Length > 0 : value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
