using CommandForge.Infrastructure;
using CommandForge.Infrastructure.Catalog;
using CommandForge.Infrastructure.Execution;

namespace CommandForge.Tests;

/// <summary>Tests that catalog executables resolve to the Windows directories, never a search path.</summary>
public sealed class ExecutableResolverTests
{
    [Theory]
    [InlineData("powershell")]
    [InlineData("PowerShell.exe")]
    public void PowerShell_ResolvesToWindowsPowerShell(string name)
        => Assert.Equal(
            Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
            ExecutableResolver.Resolve(name),
            ignoreCase: true);

    [Theory]
    [InlineData("ipconfig", "ipconfig.exe")]
    [InlineData("services.msc", "services.msc")]
    [InlineData("reg", "reg.exe")]
    public void SystemTools_ResolveToSystem32(string name, string fileName)
        => Assert.Equal(Path.Combine(Environment.SystemDirectory, fileName), ExecutableResolver.Resolve(name), ignoreCase: true);

    [Fact]
    public void Explorer_ResolvesToTheWindowsDirectory()
        => Assert.Equal(Path.Combine(SystemPaths.WindowsDirectory, "explorer.exe"), ExecutableResolver.Resolve("explorer"), ignoreCase: true);

    [Theory]
    [InlineData(@"C:\Tools\thing.exe")]
    [InlineData(@"tools\thing.exe")]
    [InlineData("definitely-not-a-windows-tool-4f1c")]
    [InlineData("")]
    public void NonCatalogNames_AreReturnedUnchanged(string name)
        => Assert.Equal(name, ExecutableResolver.Resolve(name));

    [Fact]
    public void Winget_IsTheAppExecutionAliasOrUnchanged()
    {
        var resolved = ExecutableResolver.Resolve("winget");
        var alias = Path.Combine(SystemPaths.LocalAppData, "Microsoft", "WindowsApps", "winget.exe");

        Assert.True(
            string.Equals(resolved, "winget", StringComparison.Ordinal) || string.Equals(resolved, alias, StringComparison.OrdinalIgnoreCase),
            $"Unexpected winget path '{resolved}'.");
    }

    [Fact]
    public void EveryCatalogExecutable_ResolvesInsideWindowsOrStaysBare()
    {
        // Client-only tools (rstrui, wsreset…) are missing on Windows Server build agents; those stay
        // bare names. Anything that does resolve must be an existing file in a trusted directory.
        var trusted = new[]
        {
            SystemPaths.System32,
            SystemPaths.WindowsDirectory,
            Path.Combine(SystemPaths.LocalAppData, "Microsoft", "WindowsApps"),
        };

        foreach (var executable in CatalogLoader.LoadEmbedded().Commands.Select(c => c.Executable).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var resolved = ExecutableResolver.Resolve(executable);
            if (string.Equals(resolved, executable, StringComparison.Ordinal))
            {
                continue;
            }

            Assert.True(File.Exists(resolved), $"{executable} resolved to missing file {resolved}.");
            var directory = Path.GetDirectoryName(resolved) ?? string.Empty;
            Assert.True(
                trusted.Any(t => directory.StartsWith(t, StringComparison.OrdinalIgnoreCase)),
                $"{executable} resolved outside the Windows directories: {resolved}.");
        }
    }
}
