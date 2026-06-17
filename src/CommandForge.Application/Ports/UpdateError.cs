namespace CommandForge.Application.Ports;

/// <summary>
/// Mapped failure categories for an update check or download (see <c>architectures.md</c> §5).
/// GitHub uses 403 (primary rate limit) and 429 (secondary) — there is no 419.
/// </summary>
public enum UpdateError
{
    /// <summary>No error.</summary>
    None = 0,

    /// <summary>No network / DNS failure / request could not reach GitHub.</summary>
    Offline,

    /// <summary>404 — the release feed or repository was not found.</summary>
    NotFound,

    /// <summary>403 or 429 — GitHub rate limit reached.</summary>
    RateLimited,

    /// <summary>5xx — GitHub or its CDN returned a server error.</summary>
    ServerError,

    /// <summary>The app is not Velopack-installed (dev/unpackaged); updates do not apply.</summary>
    NotInstalled,

    /// <summary>An unexpected status (including the mythical 419) or exception.</summary>
    Unknown,
}
