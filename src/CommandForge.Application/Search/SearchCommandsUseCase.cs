using CommandForge.Domain;

namespace CommandForge.Application.Search;

/// <summary>
/// A command paired with its resolved (localized) searchable text. The presentation layer
/// resolves resource keys to text and supplies these, keeping search logic free of i18n.
/// </summary>
public sealed record SearchableCommand(
    CommandDefinition Command,
    string Title,
    string Description,
    IReadOnlyList<string> Tags);

/// <summary>Optional filters applied before fuzzy ranking.</summary>
public sealed record CommandSearchFilter
{
    public string? CategoryId { get; init; }
    public IReadOnlySet<DangerLevel>? DangerLevels { get; init; }
    public bool? RequiresAdmin { get; init; }
    public bool? RequiresRestart { get; init; }
    public bool? Revertable { get; init; }

    /// <summary>Whether <paramref name="command"/> passes all active filters.</summary>
    public bool Matches(CommandDefinition command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (CategoryId is not null && !string.Equals(command.CategoryId, CategoryId, StringComparison.Ordinal))
        {
            return false;
        }

        if (DangerLevels is { Count: > 0 } levels && !levels.Contains(command.DangerLevel))
        {
            return false;
        }

        if (RequiresAdmin is { } admin && command.RequiresAdmin != admin)
        {
            return false;
        }

        if (RequiresRestart is { } restart && (command.Restart != RestartPolicy.No) != restart)
        {
            return false;
        }

        if (Revertable is { } revertable && (command.RevertCommandId is not null) != revertable)
        {
            return false;
        }

        return true;
    }
}

/// <summary>A search result: the command, its relevance score, and matched title indices for highlight.</summary>
public sealed record CommandSearchHit(CommandDefinition Command, int Score, IReadOnlyList<int> TitleMatches);

/// <summary>
/// Filters and fuzzy-ranks commands. With an empty query, returns filtered commands in input
/// order; otherwise ranks by relevance (title matches weighted above description/tags).
/// </summary>
public static class SearchCommandsUseCase
{
    private const int TitleWeight = 20;

    public static IReadOnlyList<CommandSearchHit> Search(
        IEnumerable<SearchableCommand> commands,
        string? query,
        CommandSearchFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var trimmed = query?.Trim() ?? string.Empty;
        var hasQuery = trimmed.Length > 0;
        var hits = new List<CommandSearchHit>();

        foreach (var item in commands)
        {
            if (filter is not null && !filter.Matches(item.Command))
            {
                continue;
            }

            if (!hasQuery)
            {
                hits.Add(new CommandSearchHit(item.Command, 0, []));
                continue;
            }

            var titleMatch = FuzzyMatcher.Match(item.Title, trimmed);
            var best = int.MinValue;

            if (titleMatch is { } tm)
            {
                best = tm.Score + TitleWeight;
            }

            if (FuzzyMatcher.Match(item.Description, trimmed) is { } dm)
            {
                best = Math.Max(best, dm.Score);
            }

            foreach (var tag in item.Tags)
            {
                if (FuzzyMatcher.Match(tag, trimmed) is { } tagMatch)
                {
                    best = Math.Max(best, tagMatch.Score);
                }
            }

            if (best == int.MinValue)
            {
                continue;
            }

            hits.Add(new CommandSearchHit(item.Command, best, titleMatch?.MatchedIndices ?? []));
        }

        if (hasQuery)
        {
            hits.Sort(static (a, b) => b.Score.CompareTo(a.Score));
        }

        return hits;
    }
}
