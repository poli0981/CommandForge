namespace CommandForge.Application.Search;

/// <summary>A fuzzy match: a relevance <see cref="Score"/> and the matched character indices.</summary>
public readonly record struct FuzzyMatchResult(int Score, IReadOnlyList<int> MatchedIndices);

/// <summary>
/// Lightweight accent- and case-insensitive subsequence matcher. Rewards consecutive matches and
/// matches at word boundaries, and tries every starting point so "flush" lights up the word
/// "flush" rather than the first "f" and "l" it meets. No external dependency.
/// </summary>
public static class FuzzyMatcher
{
    private const int BaseScore = 1;
    private const int ConsecutiveBonus = 5;
    private const int WordBoundaryBonus = 10;

    /// <summary>
    /// Returns the best match for <paramref name="query"/> within <paramref name="text"/>, or
    /// <see langword="null"/> if not all query characters appear in order. An empty query
    /// matches everything with score 0.
    /// </summary>
    public static FuzzyMatchResult? Match(string text, string query)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(query);

        if (query.Length == 0)
        {
            return new FuzzyMatchResult(0, []);
        }

        if (text.Length == 0)
        {
            return null;
        }

        var foldedText = TextFolding.Fold(text);
        var foldedQuery = TextFolding.Fold(query);

        FuzzyMatchResult? best = null;
        for (var start = foldedText.IndexOf(foldedQuery[0]);
             start >= 0;
             start = foldedText.IndexOf(foldedQuery[0], start + 1))
        {
            if (MatchFrom(foldedText, foldedQuery, start) is { } candidate
                && (best is null || candidate.Score > best.Value.Score))
            {
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>The smallest score a title match of <paramref name="queryLength"/> characters needs to count.</summary>
    public static int MinimumUsefulScore(int queryLength)
        => queryLength <= 1 ? BaseScore : queryLength * BaseScore + (queryLength - 1) * 4;

    private static FuzzyMatchResult? MatchFrom(string text, string query, int start)
    {
        var indices = new List<int>(query.Length);
        var score = 0;
        var textIndex = start;
        var lastMatch = -2;

        foreach (var target in query)
        {
            while (textIndex < text.Length && text[textIndex] != target)
            {
                textIndex++;
            }

            if (textIndex >= text.Length)
            {
                return null;
            }

            indices.Add(textIndex);
            score += BaseScore;
            if (textIndex == lastMatch + 1)
            {
                score += ConsecutiveBonus;
            }

            if (textIndex == 0 || !char.IsLetterOrDigit(text[textIndex - 1]))
            {
                score += WordBoundaryBonus;
            }

            lastMatch = textIndex;
            textIndex++;
        }

        return new FuzzyMatchResult(score, indices);
    }
}
