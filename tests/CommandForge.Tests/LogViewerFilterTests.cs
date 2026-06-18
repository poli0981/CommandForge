using CommandForge.Application.Logging;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Tests;

/// <summary>Tests the pure Log Viewer filter predicate (level buckets + case-insensitive text).</summary>
public sealed class LogViewerFilterTests
{
    [Theory]
    [InlineData(LogLevelFilter.All, LogLevel.Information, true)]
    [InlineData(LogLevelFilter.All, LogLevel.Verbose, true)]
    [InlineData(LogLevelFilter.Information, LogLevel.Information, true)]
    [InlineData(LogLevelFilter.Information, LogLevel.Warning, false)]
    [InlineData(LogLevelFilter.Warning, LogLevel.Warning, true)]
    [InlineData(LogLevelFilter.Error, LogLevel.Error, true)]
    [InlineData(LogLevelFilter.Error, LogLevel.Fatal, true)]
    [InlineData(LogLevelFilter.Error, LogLevel.Warning, false)]
    [InlineData(LogLevelFilter.Debug, LogLevel.Debug, true)]
    [InlineData(LogLevelFilter.Debug, LogLevel.Verbose, true)]
    [InlineData(LogLevelFilter.Debug, LogLevel.Information, false)]
    public void Matches_FiltersByLevelBucket(LogLevelFilter filter, LogLevel level, bool expected)
        => Assert.Equal(expected, LogViewerViewModel.Matches(Entry(level, "msg"), filter, null));

    [Fact]
    public void Matches_TextFilter_IsCaseInsensitive()
    {
        var entry = Entry(LogLevel.Information, "DISM restore started");
        Assert.True(LogViewerViewModel.Matches(entry, LogLevelFilter.All, "dism"));
        Assert.False(LogViewerViewModel.Matches(entry, LogLevelFilter.All, "winget"));
    }

    [Fact]
    public void Matches_CombinesLevelAndText()
    {
        var entry = Entry(LogLevel.Error, "boom");
        Assert.True(LogViewerViewModel.Matches(entry, LogLevelFilter.Error, "boom"));
        Assert.False(LogViewerViewModel.Matches(entry, LogLevelFilter.Information, "boom"));
    }

    private static LogEntryViewModel Entry(LogLevel level, string message)
        => new(new LogEntry(DateTimeOffset.Now, level, message));
}
