using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.JobObjects;

namespace CommandForge.Infrastructure.Execution;

/// <summary>
/// A process-wide job object with "kill on close": every captured command process (and anything it
/// starts) is placed in it, so when CommandForge — or the Elevator — exits or crashes, Windows ends
/// those processes instead of leaving a half-finished DISM/winget run behind. GUI targets started in
/// Launch mode are deliberately not added; they belong to the user.
/// </summary>
internal sealed class ProcessJob : IDisposable
{
    private static readonly Lazy<ProcessJob> SharedInstance = new(() => new ProcessJob());

    private readonly SafeFileHandle? _job;

    private ProcessJob()
    {
        try
        {
            _job = PInvoke.CreateJobObject(lpJobAttributes: null, lpName: null);
            var limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            limits.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
            var bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(in limits));
            if (_job.IsInvalid || !PInvoke.SetInformationJobObject(_job, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, bytes))
            {
                _job.Dispose();
                _job = null;
            }
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
            _job = null;
        }
    }

    /// <summary>The shared job for this process.</summary>
    public static ProcessJob Shared => SharedInstance.Value;

    /// <summary>Adds <paramref name="process"/> to the job. Best effort: a failure only loses the clean-up guarantee.</summary>
    public bool TryAdd(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (_job is null)
        {
            return false;
        }

        try
        {
            return PInvoke.AssignProcessToJobObject(_job, process.SafeHandle);
        }
        catch (InvalidOperationException)
        {
            return false; // the process already exited
        }
    }

    /// <inheritdoc />
    public void Dispose() => _job?.Dispose();
}
