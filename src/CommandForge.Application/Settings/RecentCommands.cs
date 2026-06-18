namespace CommandForge.Application.Settings;

/// <summary>Pure helpers for maintaining the "recently run" command-id list (most-recent first).</summary>
public static class RecentCommands
{
    /// <summary>The maximum number of recent command ids retained.</summary>
    public const int MaxItems = 10;

    /// <summary>
    /// Returns a new list with <paramref name="commandId"/> at the front (most recent), the previous
    /// entries following in order, de-duplicated, and capped at <see cref="MaxItems"/>. The input is
    /// not modified. Comparison is ordinal (command ids are invariant identifiers).
    /// </summary>
    public static IReadOnlyList<string> Add(IReadOnlyList<string> current, string commandId)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentException.ThrowIfNullOrEmpty(commandId);

        var result = new List<string>(current.Count + 1) { commandId };
        foreach (var id in current)
        {
            if (!string.Equals(id, commandId, StringComparison.Ordinal))
            {
                result.Add(id);
            }
        }

        if (result.Count > MaxItems)
        {
            result.RemoveRange(MaxItems, result.Count - MaxItems);
        }

        return result;
    }
}
