using System.Collections.Concurrent;
using CommandForge.Application.Ports;
using Serilog.Core;
using Serilog.Events;

namespace CommandForge.Infrastructure.Logging;

/// <summary>
/// A bounded, in-memory Serilog sink that also serves as the <see cref="ILogReader"/>
/// source for the Log Viewer. A single instance is shared between the Serilog pipeline
/// and the DI container.
/// </summary>
public sealed class InMemoryLogStore : ILogEventSink, ILogReader
{
    private const int Capacity = 1000;
    private readonly ConcurrentQueue<string> _entries = new();

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        var line = $"{logEvent.Timestamp:HH:mm:ss} [{logEvent.Level}] {logEvent.RenderMessage()}";
        _entries.Enqueue(line);

        while (_entries.Count > Capacity && _entries.TryDequeue(out _))
        {
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRecentEntries() => _entries.ToArray();
}
