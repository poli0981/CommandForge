using CommandForge.Wpf.Views;

namespace CommandForge.Tests;

/// <summary>Tests the off-screen guard used when restoring a saved window position.</summary>
public sealed class WindowPlacementTests
{
    // A single 1920x1080 monitor at the origin.
    private const double VsLeft = 0;
    private const double VsTop = 0;
    private const double VsWidth = 1920;
    private const double VsHeight = 1080;

    [Fact]
    public void IsOnScreen_True_ForFullyVisibleWindow()
        => Assert.True(WindowPlacement.IsOnScreen(100, 100, 800, 600, VsLeft, VsTop, VsWidth, VsHeight));

    [Fact]
    public void IsOnScreen_False_WhenTitleBarAboveTop()
        => Assert.False(WindowPlacement.IsOnScreen(100, -200, 800, 600, VsLeft, VsTop, VsWidth, VsHeight));

    [Fact]
    public void IsOnScreen_False_WhenOnlyASliverIsVisibleOnTheRight()
        => Assert.False(WindowPlacement.IsOnScreen(1900, 100, 800, 600, VsLeft, VsTop, VsWidth, VsHeight));

    [Fact]
    public void IsOnScreen_True_OnSecondaryMonitorWithNegativeLeft()
    {
        // Two monitors: a left one at negative coordinates plus the primary.
        const double virtualLeft = -1920;
        const double virtualWidth = 3840;
        Assert.True(WindowPlacement.IsOnScreen(-1800, 100, 800, 600, virtualLeft, VsTop, virtualWidth, VsHeight));
    }

    [Fact]
    public void IsOnScreen_False_WhenSavedMonitorIsNowDisconnected()
    {
        // Position was saved on a left monitor that is no longer present (virtual screen is now the primary only).
        Assert.False(WindowPlacement.IsOnScreen(-1800, 100, 800, 600, VsLeft, VsTop, VsWidth, VsHeight));
    }
}
