namespace CommandForge.Application.Logging;

/// <summary>A structured in-memory log entry shown by the Log Viewer.</summary>
public sealed record LogEntry(DateTimeOffset Timestamp, LogLevel Level, string Message);
