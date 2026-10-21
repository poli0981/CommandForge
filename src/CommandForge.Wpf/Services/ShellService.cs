using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace CommandForge.Wpf.Services;

/// <summary><see cref="IShellService"/> backed by ShellExecute.</summary>
public sealed class ShellService : IShellService
{
    /// <inheritdoc />
    public bool TryOpen(string target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        try
        {
            using var process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            Log.Warning(ex, "Could not open {Target}.", target);
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryRestartWindows()
    {
        try
        {
            var shutdown = Path.Combine(Environment.SystemDirectory, "shutdown.exe");
            using var process = Process.Start(new ProcessStartInfo(shutdown, "/r /t 0") { UseShellExecute = false, CreateNoWindow = true });
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Log.Warning(ex, "Could not start the restart.");
            return false;
        }
    }
}
