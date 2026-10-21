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
/// <remarks>
/// A burst of log events raises <see cref="EntriesChanged"/> once, shortly after the burst starts,
/// so the Log Viewer re-reads the buffer a few times a second instead of once per event.
/// </remarks>
public sealed class InMemoryLogStore : ILogEventSink, ILogReader, IDisposable
{
    private const int Capacity = 1000;
    private static readonly TimeSpan DefaultNotifyDelay = TimeSpan.FromMilliseconds(150);

    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly Timer _notifyTimer;
    private readonly TimeSpan _notifyDelay;
    private int _notifyPending;
    private volatile bool _disposed;

    public InMemoryLogStore()
        : this(DefaultNotifyDelay)
    {
    }

    internal InMemoryLogStore(TimeSpan notifyDelay)
    {
        _notifyDelay = notifyDelay;
        _notifyTimer = new Timer(_ => RaiseEntriesChanged(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <inheritdoc />
    public event EventHandler? EntriesChanged;

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        _entries.Enqueue(new LogEntry(logEvent.Timestamp, MapLevel(logEvent.Level), logEvent.RenderMessage()));

        TrimTo(Capacity);

        // The first event of a burst arms the timer; the rest ride along with that notification.
        if (!_disposed && Interlocked.Exchange(ref _notifyPending, 1) == 0)
        {
            _notifyTimer.Change(_notifyDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> GetRecentEntries() => _entries.ToArray();

    /// <inheritdoc />
    public void Clear()
    {
        TrimTo(0);

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _notifyTimer.Dispose();
    }

    private void RaiseEntriesChanged()
    {
        // Reset before notifying: an event that lands while handlers read the buffer arms a new round.
        Volatile.Write(ref _notifyPending, 0);
        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TrimTo(int capacity)
    {
        var removed = true;
        while (removed && _entries.Count > capacity)
        {
            removed = _entries.TryDequeue(out _);
        }
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
