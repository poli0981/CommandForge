using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommandForge.Desktop.Converters;

/// <summary>
/// Visible when the value is not <see langword="null"/> (or, with <see cref="Inverse"/>, when it is).
/// Usable inline: <c>Converter={conv:NullToVisibilityConverter Inverse=True}</c>.
/// </summary>
public sealed class NullToVisibilityConverter : MarkupExtension, IValueConverter
{
    public bool Inverse { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is not null) != Inverse ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
