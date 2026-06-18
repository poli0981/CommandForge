using System.Collections.Concurrent;
using CommandForge.Application.Logging;
using CommandForge.Application.Ports;
using Serilog.Core;
using Serilog.Events;

namespace CommandForge.Infrastructure.Logging;

/// <summary>
/// A bounded, in-memory Serilog sink that also serves as the <see cref="ILogReader"/>
/// source for the Log Viewer. A single instance is shared between the Serilog pipeline
/// and the DI container. <see cref="EntriesChanged"/> may fire off the UI thread —
/// consumers must marshal to the UI thread before touching UI state.
/// </summary>
public sealed class InMemoryLogStore : ILogEventSink, ILogReader
{
    private const int Capacity = 1000;
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    /// <inheritdoc />
    public event EventHandler? EntriesChanged;

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        _entries.Enqueue(new LogEntry(logEvent.Timestamp, MapLevel(logEvent.Level), logEvent.RenderMessage()));

        while (_entries.Count > Capacity && _entries.TryDequeue(out _))
        {
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> GetRecentEntries() => _entries.ToArray();

    /// <inheritdoc />
    public void Clear()
    {
        while (_entries.TryDequeue(out _))
        {
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    private static LogLevel MapLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LogLevel.Verbose,
        LogEventLevel.Debug => LogLevel.Debug,
        LogEventLevel.Information => LogLevel.Information,
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Error => LogLevel.Error,
        LogEventLevel.Fatal => LogLevel.Fatal,
        _ => LogLevel.Information,
    };
}
