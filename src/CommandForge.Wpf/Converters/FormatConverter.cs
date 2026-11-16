using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommandForge.Desktop.Converters;

/// <summary>
/// Fills a localized composite format with the other bound values: the first binding is the format
/// (for example <c>{res:Loc}</c>'s indexer binding to "Move {0} up"), the rest are its arguments.
/// Used for accessible names that must say which row a repeated button acts on.
/// </summary>
public sealed class FormatConverter : MarkupExtension, IMultiValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [string format, .. var rest])
        {
            return string.Empty;
        }

        var args = rest.Select(v => v == DependencyProperty.UnsetValue ? string.Empty : v).ToArray();
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
