namespace CommandForge.Application.Search;

/// <summary>A fuzzy match: a relevance <see cref="Score"/> and the matched character indices.</summary>
public readonly record struct FuzzyMatchResult(int Score, IReadOnlyList<int> MatchedIndices);

/// <summary>
/// Lightweight case-insensitive subsequence fuzzy matcher. Rewards consecutive matches and
/// matches at word boundaries so the most relevant results rank first. No external dependency.
/// </summary>
public static class FuzzyMatcher
{
    private const int BaseScore = 1;
    private const int ConsecutiveBonus = 5;
    private const int WordBoundaryBonus = 10;

    /// <summary>
    /// Returns a match for <paramref name="query"/> within <paramref name="text"/>, or
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

        var indices = new List<int>(query.Length);
        var score = 0;
        var textIndex = 0;
        var lastMatch = -2;

        foreach (var queryChar in query)
        {
            var found = false;
            for (; textIndex < text.Length; textIndex++)
            {
                if (char.ToLowerInvariant(text[textIndex]) != char.ToLowerInvariant(queryChar))
                {
                    continue;
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
                found = true;
                break;
            }

            if (!found)
            {
                return null;
            }
        }

        return new FuzzyMatchResult(score, indices);
    }
}
