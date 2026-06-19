using CommandForge.Application.History;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>Tests the pure execution-history helper (newest-first, cap, no de-dupe, no mutation).</summary>
public sealed class ExecutionHistoryTests
{
    private static ExecutionRecord Rec(string id) => new()
    {
        CommandId = id,
        Timestamp = DateTimeOffset.UnixEpoch,
        ExitCode = 0,
        Success = true,
        DurationMs = 10,
    };

    [Fact]
    public void Add_PutsNewestFirst()
    {
        var result = ExecutionHistory.Add([Rec("a")], Rec("b"));

        Assert.Equal(["b", "a"], result.Select(r => r.CommandId));
    }

    [Fact]
    public void Add_KeepsDuplicateCommandIds()
    {
        // Unlike recent commands, every run is a distinct event — no de-duplication.
        var result = ExecutionHistory.Add([Rec("a")], Rec("a"));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Add_CapsAtMaxItems()
    {
        IReadOnlyList<ExecutionRecord> current =
            Enumerable.Range(0, ExecutionHistory.MaxItems).Select(i => Rec($"c{i}")).ToList();

        var result = ExecutionHistory.Add(current, Rec("newest"));

        Assert.Equal(ExecutionHistory.MaxItems, result.Count);
        Assert.Equal("newest", result[0].CommandId);
        Assert.DoesNotContain($"c{ExecutionHistory.MaxItems - 1}", result.Select(r => r.CommandId)); // oldest dropped
    }

    [Fact]
    public void Add_DoesNotMutateInput()
    {
        var current = new List<ExecutionRecord> { Rec("a") };

        ExecutionHistory.Add(current, Rec("b"));

        Assert.Single(current);
    }
}
