using CommandForge.Domain;
using CommandForge.Infrastructure.Registry;

namespace CommandForge.Tests;

/// <summary>Smoke tests for the read-only Win32 registry service against well-known keys.</summary>
public sealed class WindowsRegistryServiceTests
{
    [Fact]
    public void Read_KnownValue_ReturnsNonNull()
    {
        var service = new WindowsRegistryService();

        var value = service.Read(new RegistryValueRef
        {
            Path = "HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
            Name = "ProductName",
        });

        Assert.False(string.IsNullOrEmpty(value));
    }

    [Fact]
    public void Read_MissingKey_ReturnsNull()
    {
        var service = new WindowsRegistryService();

        Assert.Null(service.Read(new RegistryValueRef
        {
            Path = "HKCU\\Software\\CommandForge\\Missing_" + Guid.NewGuid().ToString("N"),
            Name = "Nope",
        }));
    }

    [Fact]
    public void Read_UnknownHive_ReturnsNull()
    {
        var service = new WindowsRegistryService();

        Assert.Null(service.Read(new RegistryValueRef { Path = "BOGUS\\Whatever", Name = "X" }));
    }
}
