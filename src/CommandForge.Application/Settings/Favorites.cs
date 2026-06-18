namespace CommandForge.Application.Settings;

/// <summary>Pure helpers for maintaining the pinned-Favorites command-id list (in pin order).</summary>
public static class Favorites
{
    /// <summary>
    /// Returns a new list with <paramref name="commandId"/> toggled: appended to the end if absent,
    /// removed if already present. The input is not modified. Comparison is ordinal.
    /// </summary>
    public static IReadOnlyList<string> Toggle(IReadOnlyList<string> current, string commandId)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentException.ThrowIfNullOrEmpty(commandId);

        if (current.Contains(commandId, StringComparer.Ordinal))
        {
            return current.Where(id => !string.Equals(id, commandId, StringComparison.Ordinal)).ToList();
        }

        return [.. current, commandId];
    }
}
