using System.Threading.Channels;
using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Elevation;

/// <summary>
/// The elevated side of the broker: reads requests from the pipe, validates each
/// <c>commandId</c> against its OWN embedded catalog (golden rule: never builds a command
/// from arbitrary input), runs it, and streams output back. Stream-based so it can be driven
/// from an in-process pipe pair in tests.
/// </summary>
internal sealed class ElevationServer
{
    private readonly ICatalogProvider _catalog;
    private readonly ICommandExecutor _executor;

    public ElevationServer(ICatalogProvider catalog, ICommandExecutor executor)
    {
        _catalog = catalog;
        _executor = executor;
    }

    public async Task RunAsync(Stream pipe, CancellationToken cancellationToken)
    {
        using var channel = new PipeMessageChannel(pipe);
        CancellationTokenSource? runCts = null;
        var runTask = Task.CompletedTask;

        try
        {
            while (true)
            {
                var request = await channel.ReadAsync(ElevationJsonContext.Default.ElevationRequest, cancellationToken);
                if (request is null)
                {
                    break; // pipe closed by the UI
                }

                switch (request.Kind)
                {
                    case ElevationRequestKind.Run:
                        runCts?.Cancel();
                        await runTask;
                        runCts?.Dispose();
                        runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        runTask = HandleRunAsync(channel, request.CommandId, runCts.Token);
                        break;

                    case ElevationRequestKind.Cancel:
                        runCts?.Cancel();
                        break;

                    default:
                        break;
                }
            }
        }
        finally
        {
            runCts?.Cancel();
            await runTask;
            runCts?.Dispose();
        }
    }

    private async Task HandleRunAsync(PipeMessageChannel channel, string? commandId, CancellationToken cancellationToken)
    {
        try
        {
            var command = commandId is null
                ? null
                : _catalog.GetCommands().FirstOrDefault(c => string.Equals(c.Id, commandId, StringComparison.Ordinal));

            if (command is null || !command.RequiresAdmin)
            {
                await SendRejectionAsync(channel, $"Rejected: '{commandId}' is not an admin command in the catalog.");
                return;
            }

            var output = Channel.CreateUnbounded<OutputLine>(new UnboundedChannelOptions { SingleReader = true });
            var execution = _executor.ExecuteAsync(command, output.Writer, cancellationToken);

            await foreach (var line in output.Reader.ReadAllAsync(CancellationToken.None))
            {
                await channel.WriteAsync(
                    ElevationMessage.Output(line.Text, line.IsError),
                    ElevationJsonContext.Default.ElevationMessage,
                    CancellationToken.None);
            }

            var result = await execution;
            await channel.WriteAsync(
                ElevationMessage.FromResult(result),
                ElevationJsonContext.Default.ElevationMessage,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            await SendRejectionAsync(channel, $"Elevator error: {ex.Message}");
        }
    }

    private static async Task SendRejectionAsync(PipeMessageChannel channel, string message)
    {
        try
        {
            await channel.WriteAsync(
                ElevationMessage.Output(message, isError: true),
                ElevationJsonContext.Default.ElevationMessage,
                CancellationToken.None);
            await channel.WriteAsync(
                new ElevationMessage { Kind = ElevationMessageKind.Result, ExitCode = -1, Success = false },
                ElevationJsonContext.Default.ElevationMessage,
                CancellationToken.None);
        }
        catch (IOException)
        {
            // Pipe already gone — nothing we can do.
        }
    }
}
