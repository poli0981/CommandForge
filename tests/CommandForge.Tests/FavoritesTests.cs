using CommandForge.Application.Settings;

namespace CommandForge.Tests;

/// <summary>Tests the Favorites pin/unpin list helper (toggle, order, immutability).</summary>
public sealed class FavoritesTests
{
    [Fact]
    public void Toggle_AddsToEnd_WhenAbsent()
    {
        IReadOnlyList<string> result = Favorites.Toggle(["a", "b"], "c");

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void Toggle_Removes_WhenPresent()
    {
        IReadOnlyList<string> result = Favorites.Toggle(["a", "b", "c"], "b");

        Assert.Equal(["a", "c"], result);
    }

    [Fact]
    public void Toggle_IsReversible()
    {
        var once = Favorites.Toggle(["a"], "b");
        var twice = Favorites.Toggle(once, "b");

        Assert.Equal(["a"], twice);
    }

    [Fact]
    public void Toggle_DoesNotMutateInput()
    {
        var current = new List<string> { "a", "b" };

        Favorites.Toggle(current, "a");

        Assert.Equal(["a", "b"], current);
    }
}
