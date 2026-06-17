using System.Globalization;
using System.Windows.Data;

namespace CommandForge.Wpf.Converters;

/// <summary>
/// Two-way converter for binding a group of <see cref="System.Windows.Controls.RadioButton"/>s to an
/// enum property, e.g. <c>IsChecked="{Binding SelectedTheme, Converter={StaticResource EnumBool}, ConverterParameter=Dark}"</c>.
/// Checking a button sets the enum; the button matching the current value stays checked.
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && parameter is string name
            && string.Equals(value.ToString(), name, StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true && parameter is string name
            ? Enum.Parse(targetType, name)
            : Binding.DoNothing;
}
