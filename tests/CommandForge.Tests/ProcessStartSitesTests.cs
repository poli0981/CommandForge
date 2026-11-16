using System.Text.RegularExpressions;
using CommandForge.Desktop.Services;

namespace CommandForge.Tests;

/// <summary>
/// Every place that starts a process was read (golden rule #1: only vetted catalog commands run, and
/// only through the elevation broker when they need admin). Qodana's security rules report a process
/// start whose arguments aren't literals, and these files are left out of that rule in qodana.yaml
/// because they were read; this tripwire fails on a new file that starts a process, so it is read too.
/// </summary>
public sealed partial class ProcessStartSitesTests
{
    /// <summary>The files read, and what each starts.</summary>
    private static readonly Dictionary<string, string> ReadSites = new(StringComparer.OrdinalIgnoreCase)
    {
        // A catalog command (resolved to a Windows directory by ExecutableResolver) or the user's own
        // non-elevated command; arguments from the vetted template and trusted tokens; no shell.
        ["src/CommandForge.Infrastructure/Execution/SystemProcessRunner.cs"] = "catalog and user commands",

        // CommandForge.Elevator.exe beside the app, with a fresh pipe name, through UAC ("runas").
        ["src/CommandForge.Infrastructure/Elevation/BrokerElevationService.cs"] = "the Elevator",

        // The browser or mail client for https:/mailto: addresses, Explorer for an existing folder
        // (anything else is refused: IsAllowedTarget), and System32\shutdown.exe /r /t 0.
        ["src/CommandForge.Wpf/Services/ShellService.cs"] = "links, folders and the restart",
    };

    [Fact]
    public void EveryFileThatStartsAProcessHasBeenRead()
    {
        var root = FindRepositoryRoot();
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var unread = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(separators).Any(part => part is "bin" or "obj"))
            .Where(path => ProcessStart().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !ReadSites.ContainsKey(path))
            .ToList();

        Assert.True(unread.Count == 0, "Read these, then list them here and in qodana.yaml (OpenGrepInspection): " + string.Join(", ", unread));
    }

    [Fact]
    public void EveryReadSiteStillExists()
    {
        var root = FindRepositoryRoot();
        Assert.All(ReadSites.Keys, path => Assert.True(File.Exists(Path.Combine(root, path)), path));
    }

    [Theory]
    [InlineData("https://github.com/poli0981/CommandForge", true)]
    [InlineData("mailto:security@poli0981.dev", true)]
    [InlineData("http://example.com", false)]
    [InlineData("file:///C:/Windows/System32/cmd.exe", false)]
    [InlineData("ms-settings:display", false)]
    [InlineData(@"C:\Windows\System32\cmd.exe", false)] // a file, not a folder
    [InlineData(@"relative\folder", false)]
    public void TheShellOpensOnlyWebPagesMailAndFolders(string target, bool allowed)
        => Assert.Equal(allowed, ShellService.IsAllowedTarget(target, _ => false));

    [Fact]
    public void TheShellOpensTheAppsOwnFolders()
        => Assert.True(ShellService.IsAllowedTarget(@"C:\Users\me\AppData\Roaming\CommandForge", _ => true));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CommandForge.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find the repository root (CommandForge.slnx) above " + AppContext.BaseDirectory);
    }

    [GeneratedRegex(@"Process\.Start\(|new ProcessStartInfo\b|ProcessStartInfo\s*\{|CreateProcess\w*\(|ShellExecute\w*\(", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 2000)]
    private static partial Regex ProcessStart();
}
