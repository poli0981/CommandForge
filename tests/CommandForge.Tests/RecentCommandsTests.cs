using CommandForge.Application.Settings;

namespace CommandForge.Tests;

/// <summary>Tests the recently-run command-id list helper (front-insert, de-dupe, cap).</summary>
public sealed class RecentCommandsTests
{
    [Fact]
    public void Add_PutsNewIdFirst()
    {
        IReadOnlyList<string> result = RecentCommands.Add(["b", "c"], "a");

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void Add_MovesExistingIdToFront_WithoutDuplicating()
    {
        IReadOnlyList<string> result = RecentCommands.Add(["a", "b", "c"], "c");

        Assert.Equal(["c", "a", "b"], result);
    }

    [Fact]
    public void Add_CapsAtMaxItems()
    {
        var current = Enumerable.Range(0, RecentCommands.MaxItems).Select(i => $"cmd{i}").ToList();

        var result = RecentCommands.Add(current, "newest");

        Assert.Equal(RecentCommands.MaxItems, result.Count);
        Assert.Equal("newest", result[0]);
        Assert.DoesNotContain($"cmd{RecentCommands.MaxItems - 1}", result); // oldest dropped
    }

    [Fact]
    public void Add_DoesNotMutateInput()
    {
        var current = new List<string> { "a", "b" };

        RecentCommands.Add(current, "c");

        Assert.Equal(["a", "b"], current);
    }
}
