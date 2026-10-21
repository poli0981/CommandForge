namespace CommandForge.Application.Ports;

/// <summary>
/// Checks GitHub Releases for application updates (via Velopack) and applies them.
/// Per-user install ⇒ no UAC; applying ends any elevated session first.
/// </summary>
public interface IUpdateService
{
    /// <summary>Whether the running app was installed via Velopack (false in dev/unpackaged).</summary>
    public bool IsUpdateSupported { get; }

    /// <summary>Checks GitHub Releases for a newer version, mapping HTTP failures to <see cref="UpdateError"/>.</summary>
    public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads (delta if available) the update found by the last successful check, reporting
    /// progress 0-100. Returns <see cref="UpdateError.None"/> once it is ready to apply.
    /// </summary>
    public Task<UpdateError> DownloadAsync(IProgress<int>? progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the elevated session (the Elevator runs from the folder being replaced), applies the
    /// downloaded update and restarts. On success the process exits and this does not return.
    /// </summary>
    public Task<UpdateError> ApplyAndRestartAsync(CancellationToken cancellationToken = default);
}
