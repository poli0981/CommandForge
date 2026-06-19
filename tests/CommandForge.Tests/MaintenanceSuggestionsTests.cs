using CommandForge.Application.Ports;
using CommandForge.Application.SystemInfo;

namespace CommandForge.Tests;

/// <summary>Tests the local maintenance-suggestion heuristics over system status.</summary>
public sealed class MaintenanceSuggestionsTests
{
    private static SystemStatus Status(long driveTotal, long driveFree, TimeSpan uptime)
        => new("Windows 11", 26100, 16_000_000_000UL, 8_000_000_000UL, "C:", driveTotal, driveFree, uptime);

    [Fact]
    public void For_LowDisk_SuggestsCleanup()
    {
        var suggestions = MaintenanceSuggestions.For(Status(driveTotal: 100, driveFree: 5, uptime: TimeSpan.FromHours(1)));

        Assert.Contains(suggestions, s => s.CommandId == "cleanup.cleanmgr");
    }

    [Fact]
    public void For_HealthyDisk_NoCleanup()
        => Assert.DoesNotContain(
            MaintenanceSuggestions.For(Status(driveTotal: 100, driveFree: 50, uptime: TimeSpan.FromHours(1))),
            s => s.ReasonKey == "Maint_LowDisk");

    [Fact]
    public void For_HighUptime_SuggestsRestart()
        => Assert.Contains(
            MaintenanceSuggestions.For(Status(driveTotal: 100, driveFree: 50, uptime: TimeSpan.FromDays(8))),
            s => s.ReasonKey == "Maint_HighUptime");

    [Fact]
    public void For_FreshHealthy_NoSuggestions()
        => Assert.Empty(MaintenanceSuggestions.For(Status(driveTotal: 100, driveFree: 80, uptime: TimeSpan.FromHours(2))));
}
