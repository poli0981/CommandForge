using System.Threading.Channels;
using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>
/// Runs commands that require administrator rights through the elevation broker
/// (the <c>CommandForge.Elevator</c> helper process).
/// </summary>
public interface IElevationService
{
    /// <summary>Whether an elevated session (a connected Elevator) is currently open.</summary>
    public bool IsSessionActive { get; }

    /// <summary>Raised, on any thread, when a session starts or ends; <see cref="IsSessionActive"/> has the new state.</summary>
    public event EventHandler? SessionChanged;

    /// <summary>Runs <paramref name="command"/> elevated via the broker, streaming output lines.</summary>
    public Task<ExecutionResult> RunElevatedAsync(
        CommandDefinition command,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the elevated session and waits (briefly) for the Elevator to exit — e.g. before an
    /// update replaces the files it is running from. The next admin command starts a new session.
    /// </summary>
    public Task EndSessionAsync(CancellationToken cancellationToken = default);
}
