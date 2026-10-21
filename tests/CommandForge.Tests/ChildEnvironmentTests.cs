using System.Diagnostics;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.Elevation;

namespace CommandForge.Tests;

/// <summary>
/// Tests the environment the Elevator gives elevated children: user-defined variables that could
/// inject code or retarget paths are dropped, and system paths come from trusted sources.
/// </summary>
public sealed class ChildEnvironmentTests
{
    private static readonly string ModulesDirectory = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "Modules");

    [Theory]
    [InlineData("COR_ENABLE_PROFILING")]
    [InlineData("COR_PROFILER")]
    [InlineData("CORECLR_PROFILER_PATH")]
    [InlineData("DOTNET_STARTUP_HOOKS")]
    [InlineData("PSExecutionPolicyPreference")]
    [InlineData("SOME_TOOL_SETTING")]
    public void Apply_DropsVariablesOutsideTheAllowList(string name)
    {
        var environment = UserEnvironment();
        environment[name] = "1";

        ChildEnvironment.Apply(environment);

        Assert.DoesNotContain(environment.Keys, key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Apply_KeepsHarmlessIdentityValues_WhateverTheirCase()
    {
        var environment = UserEnvironment();

        ChildEnvironment.Apply(environment);

        Assert.Equal(@"C:\fake-user\AppData\Local\Temp", environment["TEMP"]);
        Assert.Equal("someone", environment["USERNAME"]);
    }

    [Fact]
    public void Apply_OverridesUserRedirectedSystemPaths()
    {
        var environment = UserEnvironment();

        ChildEnvironment.Apply(environment);

        Assert.Equal(SystemPaths.WindowsDirectory, environment["SystemRoot"]);
        Assert.Equal(SystemPaths.WindowsDirectory, environment["windir"]);
        Assert.Equal(SystemPaths.SystemDrive, environment["SystemDrive"]);
        Assert.Equal(Path.Combine(Environment.SystemDirectory, "cmd.exe"), environment["ComSpec"]);
    }

    [Fact]
    public void Apply_LimitsPowerShellModulesToTheSystemFolder()
    {
        var environment = UserEnvironment();

        ChildEnvironment.Apply(environment);

        Assert.Equal(ModulesDirectory, environment["PSModulePath"]);
    }

    [Fact]
    public void Apply_UsesTheMachinePathOnly()
    {
        var environment = UserEnvironment();

        ChildEnvironment.Apply(environment);

        var entries = environment["Path"]!.Split(';');
        Assert.Contains(entries, e => string.Equals(e.TrimEnd('\\'), Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, e => e.StartsWith(@"C:\fake-user", StringComparison.OrdinalIgnoreCase));
        Assert.All(entries, e => Assert.True(Path.IsPathFullyQualified(e), $"'{e}' is not a full path."));
    }

    [Fact(Timeout = 60_000)]
    public async Task PowerShell_LoadsModulesOnlyFromAdminOnlyFolders()
    {
        // Windows PowerShell 5.1 re-adds Documents\WindowsPowerShell\Modules (user-writable) when
        // PSModulePath is missing or equals the machine value. Given the narrower value it only adds
        // the Program Files module folder, which needs administrator rights to change.
        var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"))
        {
            Arguments = "-NoProfile -NonInteractive -Command \"$env:PSModulePath\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };
        ChildEnvironment.Apply(info.Environment);

        using var process = Process.Start(info)!;
        var output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, process.ExitCode);
        var allowed = new[] { ModulesDirectory, Path.Combine(SystemPaths.ProgramFiles, "WindowsPowerShell", "Modules") };
        var entries = output.Trim().Split(';', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains(ModulesDirectory, entries, StringComparer.OrdinalIgnoreCase);
        Assert.All(entries, entry => Assert.Contains(entry, allowed, StringComparer.OrdinalIgnoreCase));
    }

    private static Dictionary<string, string?> UserEnvironment() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["temp"] = @"C:\fake-user\AppData\Local\Temp",
        ["USERNAME"] = "someone",
        ["windir"] = @"C:\fake-user\fake-windows",
        ["SystemRoot"] = @"C:\fake-user\fake-windows",
        ["SystemDrive"] = "Z:",
        ["ComSpec"] = @"C:\fake-user\cmd.exe",
        ["PSModulePath"] = @"C:\fake-user\Documents\WindowsPowerShell\Modules;" + ModulesDirectory,
        ["Path"] = @"C:\fake-user\bin;" + Environment.SystemDirectory,
    };
}
