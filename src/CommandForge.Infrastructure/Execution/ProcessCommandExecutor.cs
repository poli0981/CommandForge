using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Execution;

/// <summary>
/// <see cref="ICommandExecutor"/> that runs catalog commands via <see cref="IProcessRunner"/>.
/// Capture commands stream stdout/stderr through a channel; Launch commands open a GUI/shell
/// target and return immediately. Detects restart-required (exit 3010 / Always / regex).
/// </summary>
internal sealed class ProcessCommandExecutor : ICommandExecutor
{
    private readonly IProcessRunner _runner;

    public ProcessCommandExecutor(IProcessRunner runner) => _runner = runner;

    /// <inheritdoc />
    public async Task<ExecutionResult> ExecuteAsync(
        CommandDefinition command,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(output);

        var stopwatch = Stopwatch.StartNew();

        if (command.ExecutionMode == ExecutionMode.Launch)
        {
            return Launch(command, output, stopwatch);
        }

        var capturedLines = new List<string>();

        // Internal pipe: runner (background threads) -> here. We tee each line to the caller's
        // channel (for the UI) while capturing the full text for regex restart detection.
        var inner = Channel.CreateUnbounded<OutputLine>(new UnboundedChannelOptions { SingleReader = true });
        var runTask = _runner.RunAsync(command.Executable, command.ArgsTemplate, inner.Writer, cancellationToken);

        await foreach (var line in inner.Reader.ReadAllAsync(CancellationToken.None))
        {
            capturedLines.Add(line.Text);
            await output.WriteAsync(line, CancellationToken.None);
        }

        var cancelled = false;
        var exitCode = -1;
        try
        {
            exitCode = await runTask;
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            var error = $"Failed to start '{command.Executable}': {ex.Message}";
            capturedLines.Add(error);
            await output.WriteAsync(new OutputLine(error, IsError: true), CancellationToken.None);
        }
        finally
        {
            output.TryComplete();
        }

        stopwatch.Stop();
        var requiresRestart = !cancelled && DetectRestart(command, exitCode, string.Join('\n', capturedLines));

        return new ExecutionResult
        {
            ExitCode = exitCode,
            Success = !cancelled && (exitCode == 0 || exitCode == ExecutionResult.RebootRequiredExitCode),
            Duration = stopwatch.Elapsed,
            RequiresRestart = requiresRestart,
            Cancelled = cancelled,
            OutputLines = capturedLines,
        };
    }

    private ExecutionResult Launch(CommandDefinition command, ChannelWriter<OutputLine> output, Stopwatch stopwatch)
    {
        string message;
        bool success;
        try
        {
            _runner.Launch(command.Executable, command.ArgsTemplate);
            message = $"Launched: {command.Executable} {command.ArgsTemplate}".Trim();
            success = true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            message = $"Failed to launch '{command.Executable}': {ex.Message}";
            success = false;
        }

        stopwatch.Stop();
        output.TryWrite(new OutputLine(message, IsError: !success));
        output.TryComplete();

        return new ExecutionResult
        {
            ExitCode = success ? 0 : -1,
            Success = success,
            Duration = stopwatch.Elapsed,
            RequiresRestart = false,
            Cancelled = false,
            OutputLines = [message],
        };
    }

    private static bool DetectRestart(CommandDefinition command, int exitCode, string output)
        => command.Restart switch
        {
            RestartPolicy.Always => true,
            RestartPolicy.FromExitCode => exitCode == ExecutionResult.RebootRequiredExitCode,
            RestartPolicy.FromOutputRegex => command.RestartRegex is { } pattern && Regex.IsMatch(output, pattern),
            _ => false,
        };
}
