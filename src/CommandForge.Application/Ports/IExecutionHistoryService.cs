using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>
/// Persists a local history of finished command executions (newest first). All data stays on
/// the machine (golden rule: no telemetry).
/// </summary>
public interface IExecutionHistoryService
{
    /// <summary>The recorded executions, newest first.</summary>
    public IReadOnlyList<ExecutionRecord> GetRecent();

    /// <summary>Appends a record (newest first), trims to the cap, and persists.</summary>
    public void Record(ExecutionRecord record);

    /// <summary>Clears all history and persists.</summary>
    public void Clear();
}
