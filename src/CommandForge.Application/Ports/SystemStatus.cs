namespace CommandForge.Application.Ports;

/// <summary>
/// A read-only snapshot of basic system status shown on the Home dashboard. All values are read
/// locally with no elevation and nothing leaves the machine (golden rule: no telemetry).
/// </summary>
/// <param name="OsName">Friendly OS name, e.g. <c>"Windows 11"</c>.</param>
/// <param name="OsBuild">OS build number, e.g. <c>26100</c>.</param>
/// <param name="TotalMemoryBytes">Total physical RAM in bytes (0 if unavailable).</param>
/// <param name="AvailableMemoryBytes">Currently available physical RAM in bytes (0 if unavailable).</param>
/// <param name="SystemDriveName">The system drive, e.g. <c>"C:"</c>.</param>
/// <param name="SystemDriveTotalBytes">Total size of the system drive in bytes (0 if unavailable).</param>
/// <param name="SystemDriveFreeBytes">Free space on the system drive in bytes (0 if unavailable).</param>
/// <param name="Uptime">Time since the machine last booted.</param>
public sealed record SystemStatus(
    string OsName,
    int OsBuild,
    ulong TotalMemoryBytes,
    ulong AvailableMemoryBytes,
    string SystemDriveName,
    long SystemDriveTotalBytes,
    long SystemDriveFreeBytes,
    TimeSpan Uptime);
