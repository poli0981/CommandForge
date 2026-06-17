namespace CommandForge.Application.Ports;

/// <summary>
/// Reads recent in-memory log entries for the Log Viewer.
/// </summary>
public interface ILogReader
{
    /// <summary>Returns the most recent rendered log lines, oldest first.</summary>
    public IReadOnlyList<string> GetRecentEntries();
}
