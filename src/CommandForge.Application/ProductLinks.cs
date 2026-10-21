namespace CommandForge.Application;

/// <summary>
/// The project's canonical public links, in one place so the update source, About, Settings and
/// the bug-report flow can never drift apart.
/// </summary>
#pragma warning disable S1075 // These are the product's fixed public URLs, not environment-specific paths.
public static class ProductLinks
{
    /// <summary>The GitHub repository (also the Velopack update source).</summary>
    public const string Repository = "https://github.com/poli0981/CommandForge";

    /// <summary>New-issue form used by "Report a bug".</summary>
    public const string NewIssue = Repository + "/issues/new";

    /// <summary>Releases page (downloads, checksums, release notes).</summary>
    public const string Releases = Repository + "/releases";

    /// <summary>The end-user guide in the repository.</summary>
    public const string UserGuide = Repository + "/blob/main/USAGE.md";

    /// <summary>Private vulnerability reporting.</summary>
    public const string SecurityAdvisory = Repository + "/security/advisories/new";

    /// <summary>Full text of the GNU GPL v3.</summary>
    public const string License = "https://www.gnu.org/licenses/gpl-3.0.html";
}
#pragma warning restore S1075
