using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading.Channels;
using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Elevation;

/// <summary>
/// The un-elevated side of the broker. Owns the named-pipe server; lazily spawns the elevated
/// <c>CommandForge.Elevator.exe</c> once per session (one UAC prompt) and reuses it. Sends only
/// a <c>commandId</c> per request and relays streamed output back to the caller's channel.
/// </summary>
internal sealed class BrokerElevationService : IElevationService, IDisposable
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private NamedPipeServerStream? _server;
    private PipeMessageChannel? _channel;
    private Process? _elevator;

    public bool IsElevationAvailable => OperatingSystem.IsWindows();

    public async Task<ExecutionResult> RunElevatedAsync(
        CommandDefinition command,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(output);

        PipeMessageChannel channel;
        try
        {
            channel = await EnsureConnectedAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is Win32Exception or TimeoutException or IOException or InvalidOperationException)
        {
            return await FailAsync(output, $"Elevation unavailable: {ex.Message}");
        }

        using var cancelRegistration = cancellationToken.Register(
            static state => _ = SendCancelAsync((PipeMessageChannel)state!),
            channel);

        try
        {
            await channel.WriteAsync(
                new ElevationRequest { Kind = ElevationRequestKind.Run, CommandId = command.Id },
                ElevationJsonContext.Default.ElevationRequest,
                CancellationToken.None);

            while (true)
            {
                var message = await channel.ReadAsync(ElevationJsonContext.Default.ElevationMessage, CancellationToken.None);
                if (message is null)
                {
                    ResetConnection();
                    return await FailAsync(output, "The Elevator closed the connection unexpectedly.");
                }

                if (message.Kind == ElevationMessageKind.Output)
                {
                    await output.WriteAsync(new OutputLine(message.Text ?? string.Empty, message.IsError), CancellationToken.None);
                    continue;
                }

                output.TryComplete();
                return new ExecutionResult
                {
                    ExitCode = message.ExitCode,
                    Success = message.Success,
                    Duration = TimeSpan.FromMilliseconds(message.DurationMs),
                    RequiresRestart = message.RequiresRestart,
                    Cancelled = message.Cancelled,
                };
            }
        }
        catch (IOException ex)
        {
            ResetConnection();
            return await FailAsync(output, $"Elevation pipe error: {ex.Message}");
        }
    }

    private async Task<PipeMessageChannel> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null && _server is { IsConnected: true })
        {
            return _channel;
        }

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is not null && _server is { IsConnected: true })
            {
                return _channel;
            }

            ResetConnection();

            var pipeName = ElevationProtocol.CreatePipeName();
            var server = NamedPipeFactory.CreateServer(pipeName);
            try
            {
                var waitForConnection = server.WaitForConnectionAsync(cancellationToken);

                var elevatorPath = Path.Combine(AppContext.BaseDirectory, "CommandForge.Elevator.exe");
                _elevator = Process.Start(new ProcessStartInfo
                {
                    FileName = elevatorPath,
                    Arguments = pipeName,
                    UseShellExecute = true,
                    Verb = "runas",
                }) ?? throw new InvalidOperationException("Failed to start the Elevator process.");

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ConnectTimeout);
                try
                {
                    await waitForConnection.WaitAsync(timeout.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("The Elevator did not connect in time.");
                }

                _server = server;
                _channel = new PipeMessageChannel(server);
                return _channel;
            }
            catch
            {
                server.Dispose();
                _elevator?.Dispose();
                _elevator = null;
                throw;
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private static async Task SendCancelAsync(PipeMessageChannel channel)
    {
        try
        {
            await channel.WriteAsync(
                new ElevationRequest { Kind = ElevationRequestKind.Cancel },
                ElevationJsonContext.Default.ElevationRequest,
                CancellationToken.None);
        }
        catch (IOException)
        {
            // The command already finished and the pipe is gone — nothing to cancel.
        }
        catch (ObjectDisposedException)
        {
            // Connection was reset.
        }
    }

    private static async Task<ExecutionResult> FailAsync(ChannelWriter<OutputLine> output, string message)
    {
        await output.WriteAsync(new OutputLine(message, IsError: true), CancellationToken.None);
        output.TryComplete();
        return new ExecutionResult { ExitCode = -1, Success = false, Duration = TimeSpan.Zero };
    }

    private void ResetConnection()
    {
        _channel?.Dispose();
        _channel = null;

        // Closing the pipe makes the Elevator read EOF and exit on its own.
        _server?.Dispose();
        _server = null;

        _elevator?.Dispose();
        _elevator = null;
    }

    public void Dispose()
    {
        ResetConnection();
        _connectLock.Dispose();
    }
}
