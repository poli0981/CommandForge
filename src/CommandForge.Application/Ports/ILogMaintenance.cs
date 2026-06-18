namespace CommandForge.Application.Ports;

/// <summary>Maintenance over the local log files. Everything stays on the machine (no telemetry).</summary>
public interface ILogMaintenance
{
    /// <summary>The directory holding the rolling log files.</summary>
    public string LogsDirectoryPath { get; }

    /// <summary>Total size of the log files, in bytes.</summary>
    public long GetLogsSizeBytes();

    /// <summary>Exports the log files into a <c>.zip</c> at <paramref name="destinationZipPath"/>.</summary>
    public Task ExportZipAsync(string destinationZipPath, CancellationToken cancellationToken = default);

    /// <summary>Deletes rolled (non-current) log files, keeping today's active file.</summary>
    public void DeleteRolledLogFiles();
}
