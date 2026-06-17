using System.Threading.Channels;
using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Application.UseCases;

/// <summary>
/// Routes a command to the right executor: admin commands go through the elevation broker,
/// everything else runs in-process (golden rule #2 — elevate on demand, not the whole app).
/// </summary>
public sealed class RunCommandUseCase
{
    private readonly ICommandExecutor _executor;
    private readonly IElevationService _elevation;

    public RunCommandUseCase(ICommandExecutor executor, IElevationService elevation)
    {
        _executor = executor;
        _elevation = elevation;
    }

    /// <summary>Runs <paramref name="command"/>, streaming output, choosing executor by <c>RequiresAdmin</c>.</summary>
    public Task<ExecutionResult> RunAsync(
        CommandDefinition command,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.RequiresAdmin
            ? _elevation.RunElevatedAsync(command, output, cancellationToken)
            : _executor.ExecuteAsync(command, output, cancellationToken);
    }
}
