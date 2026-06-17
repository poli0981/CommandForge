namespace CommandForge.Application.Ports;

/// <summary>
/// Checks GitHub Releases for application updates (via Velopack) and applies them.
/// Per-user install ⇒ no UAC, so it never interacts with the elevation broker.
/// </summary>
public interface IUpdateService
{
    /// <summary>Whether the running app was installed via Velopack (false in dev/unpackaged).</summary>
    public bool IsUpdateSupported { get; }

    /// <summary>Checks GitHub Releases for a newer version, mapping HTTP failures to <see cref="UpdateError"/>.</summary>
    public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads (delta if available) and applies the pending update from the last successful check,
    /// then restarts. Reports progress 0-100 via <paramref name="progress"/>. Returns an
    /// <see cref="UpdateError"/> on failure; on success the process exits and this does not return.
    /// </summary>
    public Task<UpdateError> DownloadAndApplyAsync(IProgress<int>? progress, CancellationToken cancellationToken = default);
}
