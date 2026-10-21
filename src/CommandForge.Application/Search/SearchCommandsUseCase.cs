using CommandForge.Domain;

namespace CommandForge.Application.Search;

/// <summary>
/// A command paired with its resolved (localized) searchable text. The presentation layer
/// resolves resource keys to text and supplies these, keeping search logic free of i18n. The
/// accent-/case-folded forms are computed once here, not on every keystroke.
/// </summary>
public sealed record SearchableCommand(
    CommandDefinition Command,
    string Title,
    string Description,
    IReadOnlyList<string> Tags)
{
    internal string FoldedTitle { get; } = TextFolding.Fold(Title);

    internal string FoldedDescription { get; } = TextFolding.Fold(Description);

    internal IReadOnlyList<string> FoldedTags { get; } = Tags.Select(TextFolding.Fold).ToList();
}

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

        // "Requires restart" means always; exit-code/regex policies only restart sometimes.
        if (RequiresRestart is { } restart && (command.Restart == RestartPolicy.Always) != restart)
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
/// Filters and ranks commands. With an empty query, returns filtered commands in input order;
/// otherwise ranks by relevance. Titles allow fuzzy abbreviations ("sfc", "flsh dns"); descriptions
/// and tags need the query words as real substrings — matching them as loose subsequences made a
/// short query like "dns" hit most of the catalog. Matching ignores case and accents.
/// </summary>
public static class SearchCommandsUseCase
{
    private const int ExactTitleScore = 300;
    private const int TitleSubstringScore = 200;
    private const int TitleWordsScore = 160;
    private const int TitleFuzzyBase = 100;
    private const int TagScore = 60;
    private const int DescriptionScore = 40;
    private const int WordStartBonus = 30;

    public static IReadOnlyList<CommandSearchHit> Search(
        IEnumerable<SearchableCommand> commands,
        string? query,
        CommandSearchFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var trimmed = query?.Trim() ?? string.Empty;
        var filtered = filter is null ? commands : commands.Where(c => filter.Matches(c.Command));
        if (trimmed.Length == 0)
        {
            return filtered.Select(c => new CommandSearchHit(c.Command, 0, [])).ToList();
        }

        var foldedQuery = TextFolding.Fold(trimmed);
        var words = foldedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var hits = new List<CommandSearchHit>();
        foreach (var item in filtered)
        {
            var (titleScore, titleIndices) = MatchTitle(item, trimmed, foldedQuery, words);
            var score = Math.Max(titleScore, Math.Max(MatchTags(item, words), MatchDescription(item, words)));
            if (score > 0)
            {
                hits.Add(new CommandSearchHit(item.Command, score, titleIndices));
            }
        }

        // OrderByDescending is stable: equal scores keep catalog order.
        return hits.OrderByDescending(h => h.Score).ToList();
    }

    private static (int Score, IReadOnlyList<int> Indices) MatchTitle(SearchableCommand item, string query, string foldedQuery, string[] words)
    {
        var title = item.FoldedTitle;
        if (string.Equals(title, foldedQuery, StringComparison.Ordinal))
        {
            return (ExactTitleScore, Enumerable.Range(0, title.Length).ToList());
        }

        var index = BestOccurrence(title, foldedQuery);
        if (index >= 0)
        {
            var score = TitleSubstringScore + (IsWordStart(title, index) ? WordStartBonus : 0) - Math.Min(index, 20);
            return (score, Enumerable.Range(index, foldedQuery.Length).ToList());
        }

        if (words.Length > 1)
        {
            var indices = new List<int>();
            var score = TitleWordsScore;
            foreach (var word in words)
            {
                var at = BestOccurrence(title, word);
                if (at < 0)
                {
                    indices.Clear();
                    break;
                }

                score += IsWordStart(title, at) ? 10 : 0;
                indices.AddRange(Enumerable.Range(at, word.Length));
            }

            if (indices.Count > 0)
            {
                return (score, indices.Distinct().Order().ToList());
            }
        }

        if (FuzzyMatcher.Match(item.Title, query) is { } fuzzy
            && fuzzy.Score >= FuzzyMatcher.MinimumUsefulScore(foldedQuery.Length))
        {
            return (TitleFuzzyBase + fuzzy.Score, fuzzy.MatchedIndices);
        }

        return (0, []);
    }

    /// <summary>Every query word must start (or exactly equal) some tag.</summary>
    private static int MatchTags(SearchableCommand item, string[] words)
    {
        var score = 0;
        foreach (var word in words)
        {
            var best = 0;
            foreach (var tag in item.FoldedTags)
            {
                if (string.Equals(tag, word, StringComparison.Ordinal))
                {
                    best = Math.Max(best, TagScore + 10);
                }
                else if (tag.StartsWith(word, StringComparison.Ordinal))
                {
                    best = Math.Max(best, TagScore);
                }
            }

            if (best == 0)
            {
                return 0;
            }

            score = Math.Max(score, best);
        }

        return score;
    }

    /// <summary>Every query word must appear in the description as a real substring.</summary>
    private static int MatchDescription(SearchableCommand item, string[] words)
    {
        var description = item.FoldedDescription;
        var score = DescriptionScore;
        foreach (var word in words)
        {
            var at = BestOccurrence(description, word);
            if (at < 0)
            {
                return 0;
            }

            score += IsWordStart(description, at) ? 5 : 0;
        }

        return score;
    }

    /// <summary>The first occurrence of <paramref name="value"/> at a word start, else the first occurrence (-1 if none).</summary>
    private static int BestOccurrence(string text, string value)
    {
        var first = -1;
        for (var at = text.IndexOf(value, StringComparison.Ordinal); at >= 0; at = text.IndexOf(value, at + 1, StringComparison.Ordinal))
        {
            if (IsWordStart(text, at))
            {
                return at;
            }

            if (first < 0)
            {
                first = at;
            }
        }

        return first;
    }

    private static bool IsWordStart(string text, int index) => index == 0 || !char.IsLetterOrDigit(text[index - 1]);
}
