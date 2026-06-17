namespace CommandForge.Application.Ports;

/// <summary>
/// Checks GitHub Releases for application updates (via Velopack).
/// </summary>
public interface IUpdateService
{
    /// <summary>Returns <see langword="true"/> if a newer version is available.</summary>
    public Task<bool> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
