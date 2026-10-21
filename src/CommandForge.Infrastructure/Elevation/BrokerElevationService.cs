using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Channels;
using CommandForge.Application.Ports;
using CommandForge.Domain;
using Serilog;

namespace CommandForge.Infrastructure.Elevation;

/// <summary>
/// The un-elevated side of the broker. Owns the named-pipe server; lazily spawns the elevated
/// <c>CommandForge.Elevator.exe</c> once per session (one UAC prompt) and reuses it. Sends only
/// a <c>commandId</c> per request and relays streamed output back to the caller's channel.
/// </summary>
internal sealed class BrokerElevationService : IElevationService, IDisposable
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(60);

    // How long a cancelled command may take to wind down before the whole session is dropped.
    private static readonly TimeSpan CancelGracePeriod = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger _log = Log.ForContext<BrokerElevationService>();
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private NamedPipeServerStream? _server;
    private PipeMessageChannel? _channel;
    private Process? _elevator;

    /// <inheritdoc />
    public bool IsSessionActive => _channel is not null && _server is { IsConnected: true };

    /// <inheritdoc />
    public async Task<ExecutionResult> RunElevatedAsync(
        CommandDefinition command,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(output);

        try
        {
            return await RunCoreAsync(command, output, cancellationToken);
        }
        finally
        {
            output.TryComplete();
        }
    }

    /// <inheritdoc />
    public async Task EndSessionAsync(CancellationToken cancellationToken = default)
    {
        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            var elevator = _elevator;
            _elevator = null;
            ResetConnection(); // closing the pipe makes the Elevator stop its command and exit

            if (elevator is not null)
            {
                await WaitForExitAsync(elevator, cancellationToken);
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private async Task<ExecutionResult> RunCoreAsync(CommandDefinition command, ChannelWriter<OutputLine> output, CancellationToken cancellationToken)
    {
        PipeMessageChannel channel;
        try
        {
            channel = await EnsureConnectedAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return CancelledResult(); // cancelled while waiting for UAC / the Elevator to connect
        }
        catch (Exception ex) when (ex is Win32Exception or TimeoutException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            _log.Warning(ex, "Elevation unavailable for {CommandId}.", command.Id);
            return await FailAsync(output, $"Elevation unavailable: {ex.Message}");
        }

        // Reads end when the Elevator answers — or CancelGracePeriod after the user cancels, if it doesn't.
        using var readCancel = new CancellationTokenSource();
        using var cancelRegistration = cancellationToken.Register(() =>
        {
            _ = SendCancelAsync(channel);
            readCancel.CancelAfter(CancelGracePeriod);
        });

        try
        {
            await channel.WriteAsync(
                new ElevationRequest { Kind = ElevationRequestKind.Run, CommandId = command.Id },
                ElevationJsonContext.Default.ElevationRequest,
                CancellationToken.None);

            while (true)
            {
                var message = await channel.ReadAsync(ElevationJsonContext.Default.ElevationMessage, readCancel.Token);
                if (message is null)
                {
                    ResetConnection();
                    return await FailAsync(output, "The Elevator closed the connection unexpectedly.");
                }

                switch (message.Kind)
                {
                    case ElevationMessageKind.Output:
                        await output.WriteAsync(new OutputLine(message.Text ?? string.Empty, message.IsError), CancellationToken.None);
                        break;
                    case ElevationMessageKind.Log:
                        LogFromElevator(message);
                        break;
                    default:
                        return ToResult(message);
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            // The Elevator did not acknowledge the cancel in time; dropping the pipe makes it kill
            // the command and exit, and the next admin command starts a fresh session.
            _log.Warning(ex, "Elevator did not stop {CommandId} within {Seconds}s of cancel; ending the elevated session.", command.Id, CancelGracePeriod.TotalSeconds);
            ResetConnection();
            return CancelledResult();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or JsonException)
        {
            _log.Warning(ex, "Elevation pipe failed while running {CommandId}.", command.Id);
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

                // The ACL admits any process of this user; only the Elevator we just started may talk to us.
                if (!PeerVerifier.IsExpectedClient(server, _elevator.Id))
                {
                    throw new InvalidOperationException("An unexpected process connected to the elevation pipe.");
                }

                _server = server;
                _channel = new PipeMessageChannel(server);
                _log.Information("Elevated session started (Elevator process {ProcessId}).", _elevator.Id);
                return _channel;
            }
            catch
            {
                await server.DisposeAsync();
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

    private void LogFromElevator(ElevationMessage message)
    {
        if (message.IsError)
        {
            _log.Warning("Elevator: {Message}", message.Text);
        }
        else
        {
            _log.Information("Elevator: {Message}", message.Text);
        }
    }

    private async Task WaitForExitAsync(Process elevator, CancellationToken cancellationToken)
    {
        try
        {
            await elevator.WaitForExitAsync(cancellationToken).WaitAsync(ExitTimeout, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            _log.Warning(ex, "The Elevator did not exit within {Seconds}s.", ExitTimeout.TotalSeconds);
        }
        catch (InvalidOperationException)
        {
            // No process handle (already gone) — nothing to wait for.
        }
        finally
        {
            elevator.Dispose();
        }
    }

    private static ExecutionResult ToResult(ElevationMessage message) => new()
    {
        ExitCode = message.ExitCode,
        Success = message.Success,
        Duration = TimeSpan.FromMilliseconds(message.DurationMs),
        RequiresRestart = message.RequiresRestart,
        Cancelled = message.Cancelled,
    };

    private static ExecutionResult CancelledResult()
        => new() { ExitCode = -1, Success = false, Duration = TimeSpan.Zero, Cancelled = true };

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
