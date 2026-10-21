using CommandForge.Application.Ports;

namespace CommandForge.Application.UseCases;

/// <summary>
/// Coordinates update checks/downloads on top of <see cref="IUpdateService"/> (architectures.md §5).
/// Keeps the view-model thin and gives a Velopack-free seam for testing.
/// </summary>
public sealed class CheckForUpdateUseCase
{
    private readonly IUpdateService _updates;

    public CheckForUpdateUseCase(IUpdateService updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        _updates = updates;
    }

    /// <summary>Whether updates apply to this install (false in dev/unpackaged).</summary>
    public bool IsSupported => _updates.IsUpdateSupported;

    /// <summary>Checks GitHub Releases for a newer version.</summary>
    public Task<UpdateCheckResult> ExecuteAsync(CancellationToken cancellationToken = default)
        => _updates.CheckForUpdatesAsync(cancellationToken);

    /// <summary>Downloads the pending update.</summary>
    public Task<UpdateError> DownloadAsync(IProgress<int>? progress, CancellationToken cancellationToken = default)
        => _updates.DownloadAsync(progress, cancellationToken);

    /// <summary>Applies the downloaded update and restarts.</summary>
    public Task<UpdateError> ApplyAndRestartAsync(CancellationToken cancellationToken = default)
        => _updates.ApplyAndRestartAsync(cancellationToken);
}
