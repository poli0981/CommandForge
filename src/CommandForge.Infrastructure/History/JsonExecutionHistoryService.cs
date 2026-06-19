using System.Text.Json;
using CommandForge.Application.History;
using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.History;

/// <summary>
/// <see cref="IExecutionHistoryService"/> backed by a local <c>history.json</c>. All data stays
/// on the machine (see Privacy policy / golden rule "no telemetry").
/// </summary>
public sealed class JsonExecutionHistoryService : IExecutionHistoryService
{
    private readonly string _path;
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
        _records = ExecutionHistory.Add(_records, record);
        Save();
    }

    /// <inheritdoc />
    public void Clear()
    {
        _records = [];
        Save();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var file = new HistoryFile { Records = _records };
        var json = JsonSerializer.Serialize(file, ExecutionHistoryJsonContext.Default.HistoryFile);
        File.WriteAllText(_path, json);
    }

    private static IReadOnlyList<ExecutionRecord> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize(json, ExecutionHistoryJsonContext.Default.HistoryFile);
            return file?.Records ?? [];
        }
        catch (JsonException)
        {
            // Corrupt history: fall back to empty rather than crashing on startup.
            return [];
        }
    }
}
