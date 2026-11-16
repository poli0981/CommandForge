namespace CommandForge.Desktop.Views;

/// <summary>
/// F6 / Shift+F6 order between the window's regions (navigation pane, page, console), as in File
/// Explorer and Windows Settings: forward wraps from the last region to the first, backward the other
/// way, and from outside every region F6 starts at the first and Shift+F6 at the last.
/// </summary>
public static class FocusCycle
{
    /// <summary>Pure: the region to move to from <paramref name="current"/> (-1: none) among <paramref name="count"/>.</summary>
    public static int Next(int current, int count, bool backwards)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        if (current < 0 || current >= count)
        {
            return backwards ? count - 1 : 0;
        }

        return (current + (backwards ? count - 1 : 1)) % count;
    }
}
