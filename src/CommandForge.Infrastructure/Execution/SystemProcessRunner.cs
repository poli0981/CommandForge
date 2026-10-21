using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Execution;

/// <summary>Real <see cref="IProcessRunner"/> backed by <see cref="Process"/>.</summary>
internal sealed class SystemProcessRunner : IProcessRunner
{
    private static readonly TimeSpan DrainAfterKill = TimeSpan.FromSeconds(2);

    private readonly Action<IDictionary<string, string?>>? _configureEnvironment;
    private readonly Encoding _fallbackEncoding;

    /// <param name="configureEnvironment">
    /// Optional hook that rewrites a child's environment before it starts (the Elevator uses it to
    /// give elevated commands a clean, trusted environment).
    /// </param>
    public SystemProcessRunner(Action<IDictionary<string, string?>>? configureEnvironment = null)
    {
        _configureEnvironment = configureEnvironment;
        _fallbackEncoding = ConsoleEncodings.Oem;
    }

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
                FileName = ExecutableResolver.Resolve(fileName),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // Redirect stdin so we can close it immediately: a process that reads input (e.g. an
                // interactive "cmd" with no /c) gets EOF and exits instead of hanging forever.
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                // Pin the working directory to System32 so nothing is resolved from an
                // attacker-controlled current directory — this runner also serves elevated commands.
                WorkingDirectory = SystemPaths.System32,
            },
        };
        _configureEnvironment?.Invoke(process.StartInfo.Environment);

        try
        {
            process.Start();
            ProcessJob.Shared.TryAdd(process);

            // Signal EOF on the child's stdin right away — nothing here ever feeds it input, and
            // leaving it open lets an interactive process block indefinitely.
            process.StandardInput.Close();

            // Read the raw bytes: OutputDecoder handles OEM code pages and UTF-16 output, which a
            // StreamReader with a single fixed encoding would garble.
            var pumps = Task.WhenAll(
                PumpAsync(process.StandardOutput.BaseStream, isError: false, output),
                PumpAsync(process.StandardError.BaseStream, isError: true, output));

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TryKillTree(process);
                await Task.WhenAny(pumps, Task.Delay(DrainAfterKill, CancellationToken.None));
                throw;
            }

            await pumps;
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
            FileName = ExecutableResolver.Resolve(fileName),
            Arguments = arguments,
            UseShellExecute = true,
            WorkingDirectory = SystemPaths.System32,
        });
    }

    private async Task PumpAsync(Stream stream, bool isError, ChannelWriter<OutputLine> output)
    {
        var decoder = new OutputDecoder(_fallbackEncoding);
        var lines = new List<string>();
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken.None).ConfigureAwait(false)) > 0)
            {
                decoder.Push(buffer.AsSpan(0, read), lines);
                foreach (var line in lines)
                {
                    output.TryWrite(new OutputLine(line, isError));
                }

                lines.Clear();
            }

            if (decoder.Flush() is { } last)
            {
                output.TryWrite(new OutputLine(last, isError));
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // The pipe broke because the process was killed — nothing more to read.
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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
