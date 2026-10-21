using System.Text.RegularExpressions;

namespace CommandForge.Application;

/// <summary>
/// Placeholders a catalog <c>argsTemplate</c> may use for machine-specific paths, e.g.
/// <c>chkdsk {SystemDrive}</c>. The executor fills them from trusted OS sources — never from
/// environment variables, which a standard user can redefine even for elevated processes.
/// </summary>
public static partial class ArgumentTokens
{
    /// <summary>The Windows drive, e.g. <c>C:</c>.</summary>
    public const string SystemDrive = "{SystemDrive}";

    /// <summary>The Windows directory, e.g. <c>C:\Windows</c>.</summary>
    public const string SystemRoot = "{SystemRoot}";

    /// <summary>The current user's profile folder, e.g. <c>C:\Users\name</c>.</summary>
    public const string UserProfile = "{UserProfile}";

    /// <summary>Every supported token.</summary>
    public static IReadOnlyList<string> All { get; } = [SystemDrive, SystemRoot, UserProfile];

    /// <summary>Replaces each token present in <paramref name="values"/>; anything else is left as-is.</summary>
    public static string Expand(string template, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        foreach (var (token, value) in values)
        {
            template = template.Replace(token, value, StringComparison.Ordinal);
        }

        return template;
    }

    /// <summary>Token-like sequences (<c>{Word}</c>) in <paramref name="template"/> that are not supported tokens.</summary>
    public static IReadOnlyList<string> FindUnknown(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return TokenPattern().Matches(template)
            .Select(m => m.Value)
            .Where(token => !All.Contains(token, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    [GeneratedRegex(@"\{[A-Z][A-Za-z]+\}", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TokenPattern();
}
