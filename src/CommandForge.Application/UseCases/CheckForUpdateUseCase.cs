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

    /// <summary>Downloads and applies the pending update, then restarts.</summary>
    public Task<UpdateError> DownloadAndApplyAsync(IProgress<int>? progress, CancellationToken cancellationToken = default)
        => _updates.DownloadAndApplyAsync(progress, cancellationToken);
}
