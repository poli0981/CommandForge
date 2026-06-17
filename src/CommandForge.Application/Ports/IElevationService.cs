using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>
/// Runs commands that require administrator rights through the elevation broker
/// (the <c>CommandForge.Elevator</c> helper process).
/// </summary>
public interface IElevationService
{
    /// <summary>Whether elevation is currently available (broker reachable).</summary>
    public bool IsElevationAvailable { get; }

    /// <summary>
    /// Runs <paramref name="command"/> elevated via the broker, streaming output lines.
    /// </summary>
    public Task<ExecutionResult> RunElevatedAsync(
        CommandDefinition command,
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default);
}
