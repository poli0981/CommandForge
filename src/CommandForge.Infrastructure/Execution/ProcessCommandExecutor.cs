using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using CommandForge.Application;
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
    // Only the tail of the output is kept for regex restart detection.
    private const int MaxCapturedChars = 256 * 1024;

    // Catalog patterns are vetted, but output size is unbounded — cap the match time anyway.
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    private readonly IProcessRunner _runner;
    private readonly IReadOnlyDictionary<string, string> _tokens;

    public ProcessCommandExecutor(IProcessRunner runner)
        : this(runner, SystemPathTokens.Values)
    {
    }

    internal ProcessCommandExecutor(IProcessRunner runner, IReadOnlyDictionary<string, string> tokens)
    {
        _runner = runner;
        _tokens = tokens;
    }

    /// <inheritdoc />
    public async Task<ExecutionResult> ExecuteAsync(
        CommandDefinition command,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(output);

        var stopwatch = Stopwatch.StartNew();
        var arguments = ArgumentTokens.Expand(command.ArgsTemplate, _tokens);

        if (command.ExecutionMode == ExecutionMode.Launch)
        {
            return Launch(command, arguments, output, stopwatch);
        }

        // Keep (a bounded tail of) the text only when a restart regex needs to inspect it.
        var captured = command.Restart == RestartPolicy.FromOutputRegex ? new StringBuilder() : null;

        // Internal pipe: runner (background threads) -> here. Each line is teed to the caller's
        // channel (for the UI) and, when needed, into the capture buffer.
        var inner = Channel.CreateUnbounded<OutputLine>(new UnboundedChannelOptions { SingleReader = true });
        var runTask = _runner.RunAsync(command.Executable, arguments, inner.Writer, cancellationToken);

        await foreach (var line in inner.Reader.ReadAllAsync(CancellationToken.None))
        {
            Capture(captured, line.Text);
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
            await output.WriteAsync(new OutputLine(error, IsError: true), CancellationToken.None);
        }
        finally
        {
            output.TryComplete();
        }

        stopwatch.Stop();
        return new ExecutionResult
        {
            ExitCode = exitCode,
            Success = !cancelled && (exitCode == 0 || exitCode == ExecutionResult.RebootRequiredExitCode),
            Duration = stopwatch.Elapsed,
            RequiresRestart = !cancelled && DetectRestart(command, exitCode, captured),
            Cancelled = cancelled,
        };
    }

    private ExecutionResult Launch(CommandDefinition command, string arguments, ChannelWriter<OutputLine> output, Stopwatch stopwatch)
    {
        string message;
        bool success;
        try
        {
            _runner.Launch(command.Executable, arguments);
            message = $"Launched: {command.Executable} {arguments}".Trim();
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
            // A launched tool runs on its own; only an unconditional policy can be honoured.
            RequiresRestart = success && command.Restart == RestartPolicy.Always,
            Cancelled = false,
        };
    }

    private static void Capture(StringBuilder? captured, string line)
    {
        if (captured is null)
        {
            return;
        }

        captured.Append(line).Append('\n');
        if (captured.Length > MaxCapturedChars)
        {
            captured.Remove(0, captured.Length - MaxCapturedChars);
        }
    }

    private static bool DetectRestart(CommandDefinition command, int exitCode, StringBuilder? captured)
    {
        // 3010 is Windows' own "succeeded, restart required" signal, whatever the catalog policy says.
        if (exitCode == ExecutionResult.RebootRequiredExitCode)
        {
            return true;
        }

        return command.Restart switch
        {
            RestartPolicy.Always => true,
            RestartPolicy.FromOutputRegex => command.RestartRegex is { } pattern
                && captured is not null
                && Regex.IsMatch(captured.ToString(), pattern, RegexOptions.None, RegexTimeout),
            _ => false,
        };
    }
}
