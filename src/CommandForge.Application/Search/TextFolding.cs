using System.Globalization;
using System.Text;

namespace CommandForge.Application.Search;

/// <summary>
/// Accent- and case-insensitive text for search: "Dọn dẹp" folds to "don dep", "Đặt" to "dat".
/// Folding works one character at a time, so index i of the result is index i of the input —
/// highlighting maps matches straight back onto the displayed text.
/// </summary>
public static class TextFolding
{
    /// <summary>Folds <paramref name="text"/>, preserving its length.</summary>
    public static string Fold(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return string.Create(text.Length, text, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                span[i] = FoldChar(source[i]);
            }
        });
    }

    /// <summary>Folds a single character: lower-case, without diacritics.</summary>
    public static char FoldChar(char c)
    {
        if (c < 128)
        {
            return char.ToLowerInvariant(c);
        }

        if (c is 'đ' or 'Đ')
        {
            return 'd';
        }

        foreach (var part in c.ToString().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(part) != UnicodeCategory.NonSpacingMark)
            {
                return char.ToLowerInvariant(part);
            }
        }

        return c; // a lone combining mark stays as it is
    }
}
