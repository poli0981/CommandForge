using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace CommandForge.Desktop.Services;

/// <summary>
/// <see cref="IShellService"/> backed by ShellExecute — for the project's own web pages and mail
/// addresses and the app's own folders only. ShellExecute runs whatever it is handed: a <c>file:</c>
/// address or an unknown protocol handler is a program, so anything else is refused.
/// </summary>
public sealed class ShellService : IShellService
{
    /// <inheritdoc />
    public bool TryOpen(string target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        if (!IsAllowedTarget(target, Directory.Exists))
        {
            Log.Warning("Refused to open {Target}: only https and mailto addresses and existing folders are opened.", target);
            return false;
        }

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

    /// <summary>
    /// Pure: whether <paramref name="target"/> is a web page (<c>https:</c>), a mail address
    /// (<c>mailto:</c>) or an existing folder given by its full path.
    /// </summary>
    public static bool IsAllowedTarget(string target, Func<string, bool> folderExists)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(folderExists);

        // "C:\Users\…" parses as a file: URI too, so a path is checked as a path.
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && !uri.IsFile && !uri.IsUnc)
        {
            return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto;
        }

        return Path.IsPathFullyQualified(target) && folderExists(target);
    }
}
