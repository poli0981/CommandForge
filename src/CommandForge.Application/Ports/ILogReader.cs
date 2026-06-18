using CommandForge.Application.Logging;

namespace CommandForge.Application.Ports;

/// <summary>
/// Reads recent in-memory log entries for the Log Viewer and notifies when they change.
/// </summary>
public interface ILogReader
{
    /// <summary>Returns the most recent log entries, oldest first.</summary>
    public IReadOnlyList<LogEntry> GetRecentEntries();

    /// <summary>Raised when entries are added or cleared. May fire off the UI thread.</summary>
    public event EventHandler? EntriesChanged;

    /// <summary>Clears the in-memory buffer (does not delete log files).</summary>
    public void Clear();
}
