using System.Globalization;
using System.Windows.Data;
using CommandForge.Desktop.Resources;

namespace CommandForge.Desktop.Converters;

/// <summary>
/// The accessible name of a button repeated on every row of a list: its visible label first, then the
/// row it acts on ("Undo: Flush DNS cache"), so a screen reader user knows which row a "Undo" is for
/// and the name still starts with what is shown. Bind the row's title; set <see cref="LabelKey"/> to
/// the button's own label key.
/// </summary>
public sealed class RowActionNameConverter : IValueConverter
{
    /// <summary>The resource key of the button's visible label (for example <c>History_Undo</c>).</summary>
    public string LabelKey { get; set; } = string.Empty;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Format(Strings.Get(LabelKey), value as string ?? string.Empty);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    /// <summary>Pure: joins a button label and the row title with the localized pattern.</summary>
    public static string Format(string label, string rowTitle)
        => string.IsNullOrEmpty(rowTitle) ? label : string.Format(CultureInfo.CurrentCulture, Strings.Get("A11y_RowAction"), label, rowTitle);
}
