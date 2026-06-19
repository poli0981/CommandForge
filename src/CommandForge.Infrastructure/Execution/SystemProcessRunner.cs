using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Channels;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Execution;

/// <summary>Real <see cref="IProcessRunner"/> backed by <see cref="Process"/>.</summary>
internal sealed class SystemProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<int> RunAsync(
        string fileName,
        string arguments,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // Redirect stdin so we can close it immediately: a process that reads input (e.g. an
                // interactive "cmd" with no /c) gets EOF and exits instead of hanging forever.
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                // Pin the working directory to System32 so the CreateProcess search order can't
                // resolve a bare executable name (e.g. "dism") from an attacker-controlled CWD —
                // important because this runner also executes elevated commands via the broker.
                WorkingDirectory = Environment.SystemDirectory,
            },
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                output.TryWrite(new OutputLine(e.Data, IsError: false));
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                output.TryWrite(new OutputLine(e.Data, IsError: true));
            }
        };

        try
        {
            process.Start();

            // Signal EOF on the child's stdin right away — nothing here ever feeds it input, and
            // leaving it open lets an interactive process block indefinitely.
            process.StandardInput.Close();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TryKillTree(process);
                throw;
            }

            // Ensure the async stdout/stderr handlers have flushed before returning.
            process.WaitForExit();
            return process.ExitCode;
        }
        finally
        {
            output.TryComplete();
        }
    }

    /// <inheritdoc />
    public void Launch(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
        });
    }

    private static void TryKillTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the check and the kill.
        }
        catch (Win32Exception)
        {
            // Best-effort: the OS refused the kill (timing/permissions).
        }
    }
}
