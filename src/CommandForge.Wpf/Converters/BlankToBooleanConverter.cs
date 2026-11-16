using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommandForge.Desktop.Converters;

/// <summary>
/// True when the value is null, empty or only white space, e.g. a blank console line, which a screen
/// reader would otherwise read by its type name or not at all.
/// </summary>
public sealed class BlankToBooleanConverter : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null || (value is string text && string.IsNullOrWhiteSpace(text));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
