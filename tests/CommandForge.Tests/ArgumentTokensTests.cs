using CommandForge.Application;
using CommandForge.Domain;
using CommandForge.Infrastructure;
using CommandForge.Infrastructure.Execution;

namespace CommandForge.Tests;

/// <summary>Tests the argsTemplate path tokens and the trusted values that fill them.</summary>
public sealed class ArgumentTokensTests
{
    private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
    {
        [ArgumentTokens.SystemDrive] = "D:",
        [ArgumentTokens.SystemRoot] = @"D:\Windows",
        [ArgumentTokens.UserProfile] = @"D:\Users\someone",
    };

    [Fact]
    public void Expand_ReplacesEveryKnownToken()
        => Assert.Equal(
            @"chkdsk D: /scan & dir D:\Windows & D:\Users\someone\report.html",
            ArgumentTokens.Expand(@"chkdsk {SystemDrive} /scan & dir {SystemRoot} & {UserProfile}\report.html", Values));

    [Fact]
    public void Expand_LeavesOtherBracesAlone()
        => Assert.Equal("@{n='x';e={$_.Name}} {Unknown}", ArgumentTokens.Expand("@{n='x';e={$_.Name}} {Unknown}", Values));

    [Fact]
    public void FindUnknown_ReportsEachUnsupportedTokenOnce()
        => Assert.Equal(["{Foo}", "{ProgramFiles}"], ArgumentTokens.FindUnknown("{SystemDrive} {Foo} {ProgramFiles} {Foo}"));

    [Theory]
    [InlineData("Get-Service | Where-Object { $_.Status -eq 'Running' }")]
    [InlineData("Select-Object @{n='Created';e={$_.ConvertToDateTime($_.CreationTime)}}")]
    [InlineData("$h = @{0='zero'; 1='one'}")]
    [InlineData("{lowercase}")]
    public void FindUnknown_IgnoresPowerShellBraces(string template)
        => Assert.Empty(ArgumentTokens.FindUnknown(template));

    [Fact]
    public void TrustedValues_MatchTheOperatingSystem()
    {
        var windows = Path.GetDirectoryName(Environment.SystemDirectory)!;

        Assert.Equal(windows, SystemPathTokens.Values[ArgumentTokens.SystemRoot], ignoreCase: true);
        Assert.Equal(Path.GetPathRoot(windows)!.TrimEnd('\\'), SystemPathTokens.Values[ArgumentTokens.SystemDrive], ignoreCase: true);
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), SystemPathTokens.Values[ArgumentTokens.UserProfile], ignoreCase: true);
        Assert.All(ArgumentTokens.All, token => Assert.True(SystemPathTokens.Values.ContainsKey(token), $"No value for {token}."));
    }

    [Fact]
    public void Preview_ShowsTheExpandedCommandLine()
    {
        var command = new CommandDefinition
        {
            Id = "t.chkdsk",
            CategoryId = "c",
            TitleKey = "T",
            DescriptionKey = "D",
            Executable = "chkdsk",
            ArgsTemplate = "{SystemDrive} /scan",
        };

        Assert.Equal("chkdsk D: /scan", CommandPreview.Build(command, Values));
    }

    [Theory]
    [InlineData(@"%SystemRoot%\System32", @"\System32")]
    [InlineData(@"%WINDIR%\Temp", @"\Temp")]
    public void ExpandTrusted_UsesTheKernelWindowsDirectory(string value, string suffix)
        => Assert.Equal(SystemPaths.WindowsDirectory + suffix, SystemPaths.ExpandTrusted(value), ignoreCase: true);

    [Theory]
    [InlineData(@"%USERPROFILE%\AppData")]
    [InlineData(@"%TEMP%")]
    public void ExpandTrusted_RefusesUserControlledVariables(string value)
        => Assert.Null(SystemPaths.ExpandTrusted(value));
}
