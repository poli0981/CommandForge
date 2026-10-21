namespace CommandForge.Application.Search;

/// <summary>
/// Local natural-language search: maps everyday EN/VI phrases to a catalog search keyword
/// (e.g. "clean disk" / "dọn ổ đĩa" → "cleanup"). 100% LOCAL — no LLM, no network (golden rule #7).
/// Phrases match whole words, ignoring case and accents ("pin" is battery, "typing" is not).
/// </summary>
public static class NaturalLanguageSearch
{
    // Lower-cased phrase fragment -> canonical keyword fed back into the existing fuzzy search.
    private static readonly (string Phrase, string Keyword)[] Map =
    [
        ("free space", "cleanup"),
        ("disk space", "cleanup"),
        ("clean", "cleanup"),
        ("dọn", "cleanup"),
        ("dung lượng", "cleanup"),
        ("slow", "cleanup"),
        ("chậm", "cleanup"),
        ("internet", "network"),
        ("wifi", "network"),
        ("network", "network"),
        ("mạng", "network"),
        ("dns", "network"),
        ("update", "update"),
        ("cập nhật", "update"),
        ("battery", "battery"),
        ("pin", "battery"),
        ("driver", "driver"),
        ("trình điều khiển", "driver"),
    ];

    /// <summary>
    /// Returns a suggested search keyword for a natural-language query, or null if nothing matches
    /// (or the query already equals the keyword, so we don't suggest what the user typed).
    /// </summary>
    public static string? Suggest(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var text = TextFolding.Fold(query.Trim());
        foreach (var (phrase, keyword) in Map)
        {
            if (ContainsWholeWords(text, TextFolding.Fold(phrase))
                && !string.Equals(text, keyword, StringComparison.Ordinal))
            {
                return keyword;
            }
        }

        return null;
    }

    private static bool ContainsWholeWords(string text, string phrase)
    {
        for (var at = text.IndexOf(phrase, StringComparison.Ordinal); at >= 0; at = text.IndexOf(phrase, at + 1, StringComparison.Ordinal))
        {
            var end = at + phrase.Length;
            var startsWord = at == 0 || !char.IsLetterOrDigit(text[at - 1]);
            var endsWord = end == text.Length || !char.IsLetterOrDigit(text[end]);
            if (startsWord && endsWord)
            {
                return true;
            }
        }

        return false;
    }
}
