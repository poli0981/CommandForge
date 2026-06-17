using System.Threading.Channels;
using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>
/// Runs a command in the current (non-elevated) process, streaming each output line to the
/// supplied channel (the executor completes the writer when the process exits).
/// </summary>
public interface ICommandExecutor
{
    /// <summary>Executes <paramref name="command"/>, streaming output and returning the result.</summary>
    public Task<ExecutionResult> ExecuteAsync(
        CommandDefinition command,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken = default);
}
