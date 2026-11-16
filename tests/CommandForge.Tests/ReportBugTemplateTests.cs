using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;

namespace CommandForge.Tests;

/// <summary>
/// Tests the pure bug-report builders: the environment block, the "What happened?" text and the
/// address that opens the Bug report issue form with its fields filled in.
/// </summary>
public sealed partial class ReportBugTemplateTests
{
    [Fact]
    public void BuildEnvironment_IncludesVersionOsAndArchitecture()
    {
        var env = ReportBugViewModel.BuildEnvironment("1.2.3", "Windows 11", Architecture.X64);
        Assert.Contains("1.2.3", env, StringComparison.Ordinal);
        Assert.Contains("Windows 11", env, StringComparison.Ordinal);
        Assert.Contains("X64", env, StringComparison.Ordinal);
    }

    [Fact]
    public void WhatHappened_IsTheDescriptionAndTheCrashCode()
    {
        var text = ReportBugViewModel.BuildWhatHappened("  it broke  ", "240101-000000-ABCD");
        Assert.StartsWith("it broke", text, StringComparison.Ordinal);
        Assert.EndsWith("Error code: 240101-000000-ABCD", text, StringComparison.Ordinal);

        Assert.Equal("_Describe what happened._", ReportBugViewModel.BuildWhatHappened(" ", errorCode: null));
    }

    [Fact]
    public void IssueUrl_OpensTheBugFormWithItsFieldsFilledIn()
    {
        var url = ReportBugViewModel.BuildIssueUrl("[Crash 240101-000000-ABCD] ", "2.2.0", "Microsoft Windows 10.0.26100 (X64)", "spaces and #hash & more", "240101-000000-ABCD");
        var fields = Query(url);

        Assert.StartsWith(ProductLinks.NewIssue + "?template=bug_report.yml&", url, StringComparison.Ordinal);
        Assert.Equal("[Crash 240101-000000-ABCD] ", fields["title"]);
        Assert.Equal(ReportBugViewModel.BuildWhatHappened("spaces and #hash & more", "240101-000000-ABCD"), fields["what-happened"]);
        Assert.Equal("2.2.0", fields["app-version"]);
        Assert.Equal("Microsoft Windows 10.0.26100 (X64)", fields["windows-version"]);
        Assert.Contains("exported .zip", fields["logs"], StringComparison.Ordinal);
    }

    [Fact]
    public void IssueUrl_FillsOnlyFieldsTheFormHas()
    {
        var form = File.ReadAllText(Path.Combine(FindRepositoryRoot(), ".github", "ISSUE_TEMPLATE", ReportBugViewModel.BugReportForm));
        var ids = FieldId().Matches(form).Select(match => match.Groups["id"].Value).ToHashSet(StringComparer.Ordinal);

        var filled = Query(ReportBugViewModel.BuildIssueUrl("[Bug] ", "2.2.0", "Windows", null, null)).Keys
            .Where(field => field is not "template" and not "title");

        Assert.All(filled, field => Assert.Contains(field, ids));
    }

    [Fact]
    public void IssueUrl_KeepsALongDescriptionWithinUrlLimits()
    {
        var url = ReportBugViewModel.BuildIssueUrl("[Bug] ", "2.2.0", "Windows", new string('x', 20_000), null);
        Assert.Contains("truncated", Query(url)["what-happened"], StringComparison.Ordinal);
    }

    [Fact]
    public void ReportingACrashOpensTheBugFormForThisAppAndWindows()
    {
        var shell = new RecordingShell();
        var report = new ReportBugViewModel(new NoLogs(), new NoDialogs(), new NoFiles(), shell, errorCode: "240101-000000-ABCD", errorDetails: "it broke");

        report.OpenIssueCommand.Execute(null);

        var url = Assert.Single(shell.Opened);
        var fields = Query(url);
        Assert.StartsWith(ProductLinks.NewIssue + "?template=bug_report.yml&", url, StringComparison.Ordinal);
        Assert.Equal("[Crash 240101-000000-ABCD] ", fields["title"]);
        Assert.Equal(ReportBugViewModel.BuildWhatHappened("it broke", "240101-000000-ABCD"), fields["what-happened"]);
        Assert.Contains(fields["app-version"], report.EnvironmentText, StringComparison.Ordinal);
        Assert.StartsWith(RuntimeInformation.OSDescription, fields["windows-version"], StringComparison.Ordinal);
    }

    private static Dictionary<string, string> Query(string url)
        => new Uri(url).Query.TrimStart('?').Split('&')
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]), StringComparer.Ordinal);

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

    [GeneratedRegex(@"^\s+id:\s*(?<id>[a-z0-9-]+)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 2000)]
    private static partial Regex FieldId();

    private sealed class RecordingShell : IShellService
    {
        public List<string> Opened { get; } = [];

        public bool TryOpen(string target)
        {
            Opened.Add(target);
            return true;
        }

        public bool TryRestartWindows() => false;
    }

    private sealed class NoLogs : ILogMaintenance
    {
        public string LogsDirectoryPath => string.Empty;

        public long GetLogsSizeBytes() => 0;

        public Task ExportZipAsync(string destinationZipPath, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void DeleteRolledLogFiles()
        {
        }
    }

    private sealed class NoDialogs : IDialogService
    {
        public Task<bool> ConfirmAsync(string title, string message, DialogKind kind = DialogKind.Warning) => Task.FromResult(false);

        public Task ShowMessageAsync(string title, string message, DialogKind kind = DialogKind.Information) => Task.CompletedTask;
    }

    private sealed class NoFiles : IFileDialogService
    {
        public string? PickSaveFile(string title, string filter, string defaultFileName) => null;

        public string? PickOpenFile(string title, string filter) => null;
    }
}
