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
        using var store = new InMemoryLogStore();
        store.Emit(Event(LogEventLevel.Warning, "hello"));

        var entries = store.GetRecentEntries();
        Assert.Single(entries);
        Assert.Equal(LogLevel.Warning, entries[0].Level);
        Assert.Equal("hello", entries[0].Message);
    }

    [Fact]
    public void Emit_RespectsCapacity()
    {
        using var store = new InMemoryLogStore();
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
        using var store = new InMemoryLogStore();
        store.Emit(Event(serilogLevel, "x"));
        Assert.Equal(expected, store.GetRecentEntries()[0].Level);
    }

    [Fact]
    public void Clear_NotifiesImmediately_AndEmpties()
    {
        using var store = new InMemoryLogStore(Timeout.InfiniteTimeSpan); // only Clear can notify
        var notifications = 0;
        store.Emit(Event(LogEventLevel.Information, "x"));
        store.EntriesChanged += (_, _) => notifications++;

        store.Clear();

        Assert.Equal(1, notifications);
        Assert.Empty(store.GetRecentEntries());
    }

    [Fact]
    public async Task EntriesChanged_CoalescesABurstIntoFewNotifications()
    {
        using var store = new InMemoryLogStore(TimeSpan.FromMilliseconds(100));
        var notifications = 0;
        var notified = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.EntriesChanged += (_, _) =>
        {
            Interlocked.Increment(ref notifications);
            notified.TrySetResult();
        };

        for (var i = 0; i < 500; i++)
        {
            store.Emit(Event(LogEventLevel.Information, $"line {i}"));
        }

        await notified.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // One notification per 100 ms window, not one per event (the bound allows for a slow agent).
        Assert.InRange(Volatile.Read(ref notifications), 1, 50);
        Assert.Equal(500, store.GetRecentEntries().Count);
    }

    [Fact]
    public async Task EntriesChanged_FiresAgainForEventsAfterANotification()
    {
        using var store = new InMemoryLogStore(TimeSpan.FromMilliseconds(10));
        using var notifications = new SemaphoreSlim(0);
        store.EntriesChanged += (_, _) => notifications.Release();

        store.Emit(Event(LogEventLevel.Information, "first"));
        Assert.True(await notifications.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        store.Emit(Event(LogEventLevel.Information, "second"));
        Assert.True(await notifications.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    private static LogEvent Event(LogEventLevel level, string message)
        => new(DateTimeOffset.Now, level, exception: null, new MessageTemplateParser().Parse(message), []);
}
