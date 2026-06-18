namespace CommandForge.Wpf.Views;

/// <summary>Pure geometry helpers for restoring a saved window position safely across monitor changes.</summary>
public static class WindowPlacement
{
    /// <summary>Default amount of the window (in DIPs) that must stay grabbable on a screen.</summary>
    public const double DefaultMargin = 100;

    /// <summary>
    /// Returns whether a window at (<paramref name="left"/>, <paramref name="top"/>) with the given
    /// size keeps its title-bar area reachable inside the virtual screen. Guards against restoring a
    /// position saved on a monitor that is no longer connected (which would land off-screen).
    /// </summary>
    public static bool IsOnScreen(
        double left,
        double top,
        double width,
        double height,
        double virtualLeft,
        double virtualTop,
        double virtualWidth,
        double virtualHeight,
        double margin = DefaultMargin)
    {
        var virtualRight = virtualLeft + virtualWidth;
        var virtualBottom = virtualTop + virtualHeight;

        var horizontallyVisible = left + width - margin >= virtualLeft && left + margin <= virtualRight;
        var titleBarVisible = top >= virtualTop && top + margin <= virtualBottom;
        return horizontallyVisible && titleBarVisible;
    }
}
