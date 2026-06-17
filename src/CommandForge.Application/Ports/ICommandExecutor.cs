using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>
/// Runs a command in the current (non-elevated) process, streaming output lines.
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Executes <paramref name="command"/>, reporting each output line via <paramref name="output"/>.
    /// </summary>
    public Task<ExecutionResult> ExecuteAsync(
        CommandDefinition command,
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default);
}
