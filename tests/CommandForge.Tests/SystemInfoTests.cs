using CommandForge.Infrastructure.SystemInfo;

namespace CommandForge.Tests;

/// <summary>Smoke tests that the read-only system-status snapshot returns plausible values.</summary>
public sealed class SystemInfoTests
{
    [Fact]
    public void GetStatus_ReturnsPlausibleValues()
    {
        var status = new WindowsSystemInfoService().GetStatus();

        Assert.False(string.IsNullOrWhiteSpace(status.OsName));
        Assert.True(status.OsBuild > 0);

        Assert.True(status.TotalMemoryBytes > 0);
        Assert.True(status.AvailableMemoryBytes > 0);
        Assert.True(status.AvailableMemoryBytes <= status.TotalMemoryBytes);

        Assert.False(string.IsNullOrWhiteSpace(status.SystemDriveName));
        Assert.True(status.SystemDriveTotalBytes > 0);
        Assert.InRange(status.SystemDriveFreeBytes, 0, status.SystemDriveTotalBytes);

        Assert.True(status.Uptime >= TimeSpan.Zero);
    }
}
