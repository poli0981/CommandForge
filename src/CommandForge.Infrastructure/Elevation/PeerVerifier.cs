using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using Windows.Win32;

namespace CommandForge.Infrastructure.Elevation;

/// <summary>
/// Checks who is on the other end of the elevation pipe. The pipe's ACL admits any process of the
/// same user, so both sides also confirm the peer's identity: the UI only talks to the Elevator it
/// launched, and the Elevator only serves CommandForge.Wpf.exe from its own install folder.
/// </summary>
internal static class PeerVerifier
{
    /// <summary>File name of the UI executable the Elevator expects as its pipe server.</summary>
    public const string UiExecutableName = "CommandForge.Wpf.exe";

    /// <summary>UI side: true when the connected client is the process with <paramref name="expectedProcessId"/>.</summary>
    public static bool IsExpectedClient(NamedPipeServerStream pipe, int expectedProcessId)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return PInvoke.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var clientId)
            && clientId == (uint)expectedProcessId;
    }

    /// <summary>Elevator side: true when the pipe server is the UI executable next to this process.</summary>
    public static bool IsExpectedServer(NamedPipeClientStream pipe)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return PInvoke.GetNamedPipeServerProcessId(pipe.SafePipeHandle, out var serverId)
            && IsImage(serverId, Path.Combine(AppContext.BaseDirectory, UiExecutableName));
    }

    private static bool IsImage(uint processId, string expectedPath)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            var image = process.MainModule?.FileName;
            return image is not null
                && string.Equals(Path.GetFullPath(image), Path.GetFullPath(expectedPath), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {
            return false;
        }
    }
}
