using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using CommandForge.Application.Logging;
using CommandForge.Desktop.Resources;

namespace CommandForge.Desktop.Converters;

/// <summary>Shows a <see cref="LogLevel"/> by its localized name instead of the raw enum name.</summary>
public sealed class LogLevelToTextConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LogLevel level ? Text(level) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    /// <summary>Pure: the localized name of <paramref name="level"/>.</summary>
    public static string Text(LogLevel level) => Strings.Get(level switch
    {
        LogLevel.Verbose => "LogLevel_Verbose",
        LogLevel.Debug => "LogLevel_Debug",
        LogLevel.Warning => "LogLevel_Warning",
        LogLevel.Error or LogLevel.Fatal => "LogLevel_Error",
        _ => "LogLevel_Info",
    });
}
