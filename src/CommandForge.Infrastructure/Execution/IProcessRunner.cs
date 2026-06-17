using System.Threading.Channels;
using CommandForge.Domain;

namespace CommandForge.Infrastructure.Execution;

/// <summary>
/// Seam over the OS process API so the executor can be tested without spawning real
/// processes (see docs/Testing.md). The real implementation is <see cref="SystemProcessRunner"/>.
/// </summary>
internal interface IProcessRunner
{
    /// <summary>
    /// Runs a console process, writing each stdout/stderr line to <paramref name="output"/> and
    /// completing the writer on exit. Returns the exit code. On cancellation, kills the process
    /// tree and throws <see cref="OperationCanceledException"/>.
    /// </summary>
    public Task<int> RunAsync(
        string fileName,
        string arguments,
        ChannelWriter<OutputLine> output,
        CancellationToken cancellationToken);

    /// <summary>Launches a GUI/shell target and returns immediately (no output capture).</summary>
    public void Launch(string fileName, string arguments);
}
