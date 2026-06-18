using System.Runtime.InteropServices;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Tests;

/// <summary>Tests the pure bug-report builders (environment block + issue body + URL escaping).</summary>
public sealed class ReportBugTemplateTests
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
    public void BuildIssueBody_ContainsEnvironmentDescriptionCodeAndAttachmentNote()
    {
        var body = ReportBugViewModel.BuildIssueBody("App version: 1.0", "it broke", "240101-000000-ABCD");
        Assert.Contains("App version: 1.0", body, StringComparison.Ordinal);
        Assert.Contains("it broke", body, StringComparison.Ordinal);
        Assert.Contains("240101-000000-ABCD", body, StringComparison.Ordinal);
        Assert.Contains("## Environment", body, StringComparison.Ordinal);
        Assert.Contains("Attach the exported", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildIssueBody_UrlEscapingRoundTrips()
    {
        var body = ReportBugViewModel.BuildIssueBody("env", "spaces and #hash & more", errorCode: null);
        var escaped = Uri.EscapeDataString(body);
        Assert.Equal(body, Uri.UnescapeDataString(escaped));
    }
}
