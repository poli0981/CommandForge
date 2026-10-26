using System.Text.RegularExpressions;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.SystemInfo;
using CommandForge.Domain;
using CommandForge.Infrastructure.Catalog;
using CommandForge.Infrastructure.Elevation;

namespace CommandForge.Tests;

/// <summary>Validates the embedded command catalog (structural integrity).</summary>
public sealed class CatalogTests
{
    private static readonly CatalogLoadResult Catalog = CatalogLoader.LoadEmbedded();

    [Fact]
    public void Catalog_Loads_WithoutErrors()
    {
        Assert.Empty(Catalog.Errors);
    }

    [Fact]
    public void Catalog_HasCategoriesAndCommands()
    {
        string[] expected =
        [
            "system-maintenance", "updates-packages", "tweaks-shortcuts", "network", "cleanup", "power",
            "system-info", "diagnostics", "security-privacy", "developer", "storage", "services-startup",
            "devices", "backup-recovery", "accounts",
        ];

        Assert.Equal(expected, Catalog.Categories.OrderBy(c => c.Order).Select(c => c.Id));
        Assert.True(Catalog.Commands.Count >= 20, $"Expected >= 20 commands, got {Catalog.Commands.Count}.");
    }

    [Fact]
    public void Catalog_CategoryOrder_RunsFromOneWithoutGaps()
        => Assert.Equal(Enumerable.Range(1, Catalog.Categories.Count), Catalog.Categories.Select(c => c.Order).Order());

    [Theory]
    [InlineData(19045)] // Windows 10 22H2
    [InlineData(22631)] // Windows 11 23H2
    [InlineData(26100)] // Windows 11 24H2
    public void Catalog_EveryCategory_HasCommandsOnEverySupportedBuild(int build)
    {
        // The navigation pane hides commands for other builds, so an empty category would show 0.
        var empty = Catalog.Categories
            .Where(category => !Catalog.Commands.Any(c => c.CategoryId == category.Id && c.AppliesToOsBuild(build)))
            .Select(category => category.Id)
            .ToList();
        Assert.True(empty.Count == 0, $"Categories without commands on build {build}: {string.Join(", ", empty)}.");
    }

    [Fact]
    public void Catalog_CommandLines_AreUnique()
    {
        var duplicates = Catalog.Commands
            .GroupBy(c => (c.Executable.ToLowerInvariant(), c.ArgsTemplate))
            .Where(g => g.Count() > 1)
            .Select(g => string.Join(" = ", g.Select(c => c.Id)))
            .ToList();
        Assert.True(duplicates.Count == 0, $"Commands with the same command line: {string.Join("; ", duplicates)}.");
    }

    [Fact]
    public void Catalog_ShellLaunches_OpenOnlySettingsPagesAndControlPanelItems()
    {
        // explorer and control are launchers: they must open a settings URI, shell folder or Control
        // Panel item, never a file or program path.
        var offenders = Catalog.Commands
            .Where(c => (c.Executable == "explorer" && !Regex.IsMatch(c.ArgsTemplate, "^(ms-settings:|shell:|windowsdefender:|ms-windows-store:)", RegexOptions.None, TimeSpan.FromSeconds(1)))
                || (c.Executable == "control" && !Regex.IsMatch(c.ArgsTemplate, @"^([a-z]+\.cpl|/name Microsoft\.[A-Za-z]+)$", RegexOptions.None, TimeSpan.FromSeconds(1))))
            .Select(c => $"{c.Id} ({c.Executable} {c.ArgsTemplate})")
            .ToList();
        Assert.True(offenders.Count == 0, $"Unexpected launcher arguments: {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Catalog_RiskyCommands_RequireConfirmation()
    {
        // Golden rule #8: Caution and Dangerous commands confirm before running.
        var offenders = Catalog.Commands
            .Where(c => c.DangerLevel != DangerLevel.Safe && !c.ConfirmBeforeRun)
            .Select(c => c.Id)
            .ToList();
        Assert.True(offenders.Count == 0, $"Risky commands missing confirmation: {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Catalog_PowerShellCommands_NeverLoadProfilesOrPrompt()
    {
        // Output is redirected and stdin is closed: a prompt must fail fast instead of hanging, and a
        // user's profile script must never run (elevated or not).
        var offenders = Catalog.Commands
            .Where(c => c.Executable == "powershell" && !c.ArgsTemplate.StartsWith("-NoProfile -NonInteractive -Command ", StringComparison.Ordinal))
            .Select(c => c.Id)
            .ToList();
        Assert.True(offenders.Count == 0, $"PowerShell commands without -NoProfile -NonInteractive: {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Catalog_DoesNotUseWmic()
        => Assert.DoesNotContain(
            Catalog.Commands,
            c => (c.Executable + " " + c.ArgsTemplate).Contains("wmic", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Catalog_DoesNotHardcodeTheSystemDrive()
    {
        var offenders = Catalog.Commands
            .Where(c => Regex.IsMatch(c.ArgsTemplate, @"(?<![A-Za-z])[Cc]:(?=[\\/\s""']|$)", RegexOptions.None, TimeSpan.FromSeconds(1)))
            .Select(c => c.Id)
            .ToList();
        Assert.True(offenders.Count == 0, $"Use {ArgumentTokens.SystemDrive} instead of C: in: {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Catalog_NonShellCommands_DoNotRelyOnEnvironmentVariables()
    {
        // CreateProcess does not expand %VAR%; only PowerShell scripts may read the environment.
        var offenders = Catalog.Commands
            .Where(c => c.Executable != "powershell" && c.ArgsTemplate.Contains('%', StringComparison.Ordinal))
            .Select(c => c.Id)
            .ToList();
        Assert.True(offenders.Count == 0, $"Unexpanded environment variables in: {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Catalog_AdminScripts_ReadOnlyTrustedEnvironmentVariables()
    {
        // The Elevator passes TEMP, APPDATA, LOCALAPPDATA… through from the user, who can point them
        // anywhere — an elevated cleanup must never build a path to delete from them.
        var trusted = ChildEnvironment.TrustedValues().Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var offenders = Catalog.Commands
            .Where(c => c.RequiresAdmin)
            .SelectMany(c => Regex.Matches(c.ArgsTemplate, @"\$env:([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.None, TimeSpan.FromSeconds(1))
                .Select(m => m.Groups[1].Value)
                .Where(name => !trusted.Contains(name))
                .Select(name => $"{c.Id} ($env:{name})"))
            .ToList();
        Assert.True(offenders.Count == 0, $"Admin scripts reading user-controlled variables: {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Catalog_ArgumentTemplates_UseOnlyKnownTokens()
        => Assert.All(Catalog.Commands, c => Assert.Empty(ArgumentTokens.FindUnknown(c.ArgsTemplate)));

    [Fact]
    public void Catalog_CommandsReferencedFromCode_Exist()
    {
        var ids = Catalog.Commands.ToDictionary(c => c.Id, StringComparer.Ordinal);

        var restorePoint = Assert.Contains(RunSafety.RestorePointCommandId, ids);
        Assert.True(restorePoint.RequiresAdmin);

        var lowDisk = MaintenanceSuggestions.For(new SystemStatus("Windows", 26100, 1, 1, "C:", 100, 1, TimeSpan.FromDays(30)));
        Assert.All(lowDisk.Where(s => s.CommandId is not null), s => Assert.Contains(s.CommandId!, ids));
    }

    [Fact]
    public void Catalog_AllCommandIds_AreUnique()
    {
        var ids = Catalog.Commands.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Catalog_EveryCommand_HasKnownCategory()
    {
        var categoryIds = Catalog.Categories.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        Assert.All(Catalog.Commands, c => Assert.Contains(c.CategoryId, categoryIds));
    }

    [Fact]
    public void Catalog_RevertRefs_PointToExistingCommands()
    {
        var ids = Catalog.Commands.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var command in Catalog.Commands.Where(c => c.RevertCommandId is not null))
        {
            Assert.Contains(command.RevertCommandId!, ids);
        }
    }
}
