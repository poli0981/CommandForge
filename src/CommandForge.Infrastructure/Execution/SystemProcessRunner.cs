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
                UseShellExecute = false,
                CreateNoWindow = true,
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
