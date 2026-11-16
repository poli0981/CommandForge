using System.Text.RegularExpressions;

namespace CommandForge.Tests;

/// <summary>
/// ACCESSIBILITY.md is what a person who has just met a barrier reads: it states a goal and never a
/// conformance claim, every link in it works, and it gives a way to report a barrier.
/// </summary>
public sealed partial class AccessibilityDocumentTests
{
    private static readonly string Root = FindRepositoryRoot();
    private static readonly string Statement = File.ReadAllText(Path.Combine(Root, "ACCESSIBILITY.md"));

    [Fact]
    public void ItStatesAGoalAndNeverAConformanceClaim()
    {
        Assert.Contains("WCAG 2.2", Statement, StringComparison.Ordinal);
        Assert.Contains("not a claim", Statement, StringComparison.Ordinal);
        Assert.DoesNotMatch(ConformanceClaim(), Statement);
    }

    [Fact]
    public void EveryRepositoryLinkNamesAFileThatExists()
    {
        var broken = MarkdownLink().Matches(Statement)
            .Select(match => match.Groups["target"].Value)
            .Where(target => !target.Contains("://", StringComparison.Ordinal) && !target.StartsWith("mailto:", StringComparison.Ordinal) && !target.StartsWith('#'))
            .Select(target => target.Split('#')[0])
            .Where(path => !File.Exists(Path.Combine(Root, path)))
            .ToList();

        Assert.True(broken.Count == 0, "Links to missing files: " + string.Join(", ", broken));
    }

    [Fact]
    public void ABarrierHasAFormAndAnAddress()
    {
        Assert.Contains("issues/new?template=accessibility.yml", Statement, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(Root, ".github", "ISSUE_TEMPLATE", "accessibility.yml")));
        Assert.Contains(CommandForge.Application.ProductLinks.AccessibilityEmail, Statement, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CommandForge.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find the repository root (CommandForge.slnx) above " + AppContext.BaseDirectory);
    }

    // A sentence that would claim conformance rather than state a goal.
    [GeneratedRegex(@"\bCommandForge (?:conforms|complies|meets WCAG|is (?:fully )?(?:accessible|compliant|conformant))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 2000)]
    private static partial Regex ConformanceClaim();

    [GeneratedRegex(@"\]\((?<target>[^)\s]+)\)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 2000)]
    private static partial Regex MarkdownLink();
}
