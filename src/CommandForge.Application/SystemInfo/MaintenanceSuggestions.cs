using CommandForge.Application.Ports;

namespace CommandForge.Application.SystemInfo;

/// <summary>A maintenance hint derived from system status: a reason and an optional catalog command to run.</summary>
public sealed record MaintenanceSuggestion(string ReasonKey, string? CommandId);

/// <summary>
/// Pure, local heuristics mapping a <see cref="SystemStatus"/> to maintenance suggestions for the
/// Home dashboard. No network, no telemetry — just thresholds over the read-only status snapshot.
/// </summary>
public static class MaintenanceSuggestions
{
    /// <summary>Free system-drive space below this fraction triggers a cleanup suggestion.</summary>
    public const double LowDiskFreeFraction = 0.10;

    /// <summary>Uptime at or above this triggers a restart suggestion.</summary>
    public static readonly TimeSpan HighUptime = TimeSpan.FromDays(7);

    /// <summary>Returns the maintenance suggestions that apply to the given status (possibly empty).</summary>
    public static IReadOnlyList<MaintenanceSuggestion> For(SystemStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var suggestions = new List<MaintenanceSuggestion>();

        if (status.SystemDriveTotalBytes > 0
            && (double)status.SystemDriveFreeBytes / status.SystemDriveTotalBytes < LowDiskFreeFraction)
        {
            suggestions.Add(new MaintenanceSuggestion("Maint_LowDisk", "cleanup.cleanmgr"));
        }

        if (status.Uptime >= HighUptime)
        {
            suggestions.Add(new MaintenanceSuggestion("Maint_HighUptime", null));
        }

        return suggestions;
    }
}
