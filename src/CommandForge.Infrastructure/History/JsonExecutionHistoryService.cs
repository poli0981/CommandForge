using CommandForge.Application.History;
using CommandForge.Application.Ports;
using CommandForge.Domain;
using CommandForge.Infrastructure.Persistence;

namespace CommandForge.Infrastructure.History;

/// <summary>
/// <see cref="IExecutionHistoryService"/> backed by a local <c>history.json</c>. All data stays
/// on the machine (see Privacy policy / golden rule "no telemetry").
/// </summary>
public sealed class JsonExecutionHistoryService : IExecutionHistoryService
{
    private readonly string _path;
    private readonly Lock _gate = new();
    private IReadOnlyList<ExecutionRecord> _records;

    /// <summary>Creates a service backed by the default <see cref="AppPaths.HistoryFilePath"/>.</summary>
    public JsonExecutionHistoryService()
        : this(AppPaths.HistoryFilePath)
    {
    }

    /// <summary>Creates a service backed by an explicit path (used in tests).</summary>
    public JsonExecutionHistoryService(string path)
    {
        _path = path;
        _records = Load(path);
    }

    /// <inheritdoc />
    public IReadOnlyList<ExecutionRecord> GetRecent() => _records;

    /// <inheritdoc />
    public void Record(ExecutionRecord record)
    {
        lock (_gate)
        {
            _records = ExecutionHistory.Add(_records, record);
            Save();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            _records = [];
            Save();
        }
    }

    private void Save()
        => JsonFile.TryWrite(_path, new HistoryFile { Records = _records }, ExecutionHistoryJsonContext.Default.HistoryFile);

    private static IReadOnlyList<ExecutionRecord> Load(string path)
        => JsonFile.Read(path, ExecutionHistoryJsonContext.Default.HistoryFile)?.Records?.Where(r => r is not null).ToList() ?? [];
}
