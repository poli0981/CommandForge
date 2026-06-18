using System.Globalization;
using CommandForge.Application.Logging;

namespace CommandForge.Wpf.ViewModels;

/// <summary>A single log line for display: formatted time, 3-letter level tag, message, and raw level.</summary>
public sealed class LogEntryViewModel
{
    public LogEntryViewModel(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Level = entry.Level;
        Time = entry.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        LevelTag = ToTag(entry.Level);
        Message = entry.Message;
    }

    public LogLevel Level { get; }

    public string Time { get; }

    public string LevelTag { get; }

    public string Message { get; }

    private static string ToTag(LogLevel level) => level switch
    {
        LogLevel.Verbose => "VRB",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Fatal => "FTL",
        _ => "INF",
    };
}
