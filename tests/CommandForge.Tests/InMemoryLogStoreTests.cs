using CommandForge.Application.Logging;
using CommandForge.Infrastructure.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace CommandForge.Tests;

/// <summary>Tests the structured in-memory log store: mapping, ring buffer, change notification.</summary>
public sealed class InMemoryLogStoreTests
{
    [Fact]
    public void Emit_StoresStructuredEntry_WithMappedLevelAndMessage()
    {
        var store = new InMemoryLogStore();
        store.Emit(Event(LogEventLevel.Warning, "hello"));

        var entries = store.GetRecentEntries();
        Assert.Single(entries);
        Assert.Equal(LogLevel.Warning, entries[0].Level);
        Assert.Equal("hello", entries[0].Message);
    }

    [Fact]
    public void Emit_RespectsCapacity()
    {
        var store = new InMemoryLogStore();
        for (var i = 0; i < 1100; i++)
        {
            store.Emit(Event(LogEventLevel.Information, $"line {i}"));
        }

        Assert.True(store.GetRecentEntries().Count <= 1000);
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, LogLevel.Verbose)]
    [InlineData(LogEventLevel.Debug, LogLevel.Debug)]
    [InlineData(LogEventLevel.Information, LogLevel.Information)]
    [InlineData(LogEventLevel.Warning, LogLevel.Warning)]
    [InlineData(LogEventLevel.Error, LogLevel.Error)]
    [InlineData(LogEventLevel.Fatal, LogLevel.Fatal)]
    public void Emit_MapsSerilogLevel(LogEventLevel serilogLevel, LogLevel expected)
    {
        var store = new InMemoryLogStore();
        store.Emit(Event(serilogLevel, "x"));
        Assert.Equal(expected, store.GetRecentEntries()[0].Level);
    }

    [Fact]
    public void EntriesChanged_FiresOnEmitAndClear_AndClearEmpties()
    {
        var store = new InMemoryLogStore();
        var notifications = 0;
        store.EntriesChanged += (_, _) => notifications++;

        store.Emit(Event(LogEventLevel.Information, "x"));
        store.Clear();

        Assert.Equal(2, notifications);
        Assert.Empty(store.GetRecentEntries());
    }

    private static LogEvent Event(LogEventLevel level, string message)
        => new(DateTimeOffset.Now, level, exception: null, new MessageTemplateParser().Parse(message), []);
}
