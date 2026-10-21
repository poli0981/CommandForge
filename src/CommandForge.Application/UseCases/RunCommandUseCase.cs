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
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(elevation);
        _executor = executor;
        _elevation = elevation;
    }

    /// <summary>
    /// Runs <paramref name="command"/>, streaming output, choosing the executor by <c>RequiresAdmin</c>.
    /// The output channel is always completed — even when the run throws — so a reader never waits forever.
    /// </summary>
    public async Task<ExecutionResult> RunAsync(
        CommandDefinition command,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(output);

        try
        {
            return command.RequiresAdmin
                ? await _elevation.RunElevatedAsync(command, output, cancellationToken).ConfigureAwait(false)
                : await _executor.ExecuteAsync(command, output, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ExecutionResult { ExitCode = -1, Success = false, Duration = TimeSpan.Zero, Cancelled = true };
        }
        finally
        {
            output.TryComplete();
        }
    }
}
