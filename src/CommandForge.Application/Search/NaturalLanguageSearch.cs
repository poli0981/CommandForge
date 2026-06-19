namespace CommandForge.Application.Search;

/// <summary>
/// Local natural-language search: maps everyday EN/VI phrases to a catalog search keyword
/// (e.g. "clean disk" / "dọn ổ đĩa" → "cleanup"). 100% LOCAL — no LLM, no network (golden rule #5).
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

        var text = query.Trim().ToLowerInvariant();
        foreach (var (phrase, keyword) in Map)
        {
            if (text.Contains(phrase, StringComparison.Ordinal)
                && !string.Equals(text, keyword, StringComparison.Ordinal))
            {
                return keyword;
            }
        }

        return null;
    }
}
