using System.Runtime.InteropServices;
using CommandForge.Application.Ports;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace CommandForge.Infrastructure.SystemInfo;

/// <summary>
/// Read-only <see cref="ISystemInfoService"/> for the Home dashboard. Uses managed APIs plus a single
/// read-only Win32 call (<c>GlobalMemoryStatusEx</c>) for physical RAM — no process is spawned, no
/// elevation is required, and nothing leaves the machine.
/// </summary>
public sealed class WindowsSystemInfoService : ISystemInfoService
{
    // Windows 11 reports build numbers >= 22000; everything below is Windows 10 (this app targets 10/11 only).
    private const int Windows11MinimumBuild = 22000;

    /// <inheritdoc />
    public SystemStatus GetStatus()
    {
        var version = Environment.OSVersion.Version;
        var osName = version.Build >= Windows11MinimumBuild ? "Windows 11" : "Windows 10";

        var (totalMemory, availableMemory) = ReadPhysicalMemory();
        var (driveName, driveTotal, driveFree) = ReadSystemDrive();
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

        return new SystemStatus(
            osName,
            version.Build,
            totalMemory,
            availableMemory,
            driveName,
            driveTotal,
            driveFree,
            uptime);
    }

    private static (ulong Total, ulong Available) ReadPhysicalMemory()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        return PInvoke.GlobalMemoryStatusEx(ref status)
            ? (status.ullTotalPhys, status.ullAvailPhys)
            : (0UL, 0UL);
    }

    private static (string Name, long Total, long Free) ReadSystemDrive()
    {
        try
        {
            var systemRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            if (string.IsNullOrEmpty(systemRoot))
            {
                return ("C:", 0L, 0L);
            }

            var drive = new DriveInfo(systemRoot);
            var name = drive.Name.TrimEnd('\\', '/');
            return drive.IsReady
                ? (name, drive.TotalSize, drive.AvailableFreeSpace)
                : (name, 0L, 0L);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            // Drive not ready / inaccessible — report zeros rather than crash the dashboard.
            return ("C:", 0L, 0L);
        }
    }
}
