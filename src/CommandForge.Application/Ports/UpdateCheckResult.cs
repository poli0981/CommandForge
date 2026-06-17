namespace CommandForge.Application.Ports;

/// <summary>
/// Outcome of an update check: either an update is available, the app is current, or an error
/// occurred. Versions are formatted strings so the Application layer stays Velopack-free.
/// </summary>
public sealed record UpdateCheckResult(bool HasUpdate, string? NewVersion, UpdateError Error)
{
    /// <summary>The app is already on the latest version.</summary>
    public static UpdateCheckResult UpToDate { get; } = new(false, null, UpdateError.None);

    /// <summary>A newer <paramref name="version"/> is available.</summary>
    public static UpdateCheckResult Available(string version) => new(true, version, UpdateError.None);

    /// <summary>The check failed with the given <paramref name="error"/>.</summary>
    public static UpdateCheckResult Failed(UpdateError error) => new(false, null, error);

    /// <summary>Whether the check completed without an error (may still be up to date).</summary>
    public bool IsSuccess => Error == UpdateError.None;
}
