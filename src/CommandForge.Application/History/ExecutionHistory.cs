using CommandForge.Domain;

namespace CommandForge.Application.History;

/// <summary>
/// Pure helper for the execution-history list: newest first, capped at <see cref="MaxItems"/>.
/// Unlike recent commands, history is NOT de-duplicated — each run is a distinct event.
/// </summary>
public static class ExecutionHistory
{
    /// <summary>Maximum number of records retained (oldest are dropped beyond this).</summary>
    public const int MaxItems = 200;

    /// <summary>
    /// Returns a new list with <paramref name="record"/> prepended (newest first) and capped at
    /// <see cref="MaxItems"/>. The input list is not mutated.
    /// </summary>
    public static IReadOnlyList<ExecutionRecord> Add(IReadOnlyList<ExecutionRecord> current, ExecutionRecord record)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(record);

        var result = new List<ExecutionRecord>(Math.Min(current.Count + 1, MaxItems)) { record };
        foreach (var existing in current)
        {
            if (result.Count >= MaxItems)
            {
                break;
            }

            result.Add(existing);
        }

        return result;
    }
}
