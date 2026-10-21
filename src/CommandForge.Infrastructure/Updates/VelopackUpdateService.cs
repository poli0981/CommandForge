using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.Updates;
using Serilog;
using Velopack;
using Velopack.Sources;

namespace CommandForge.Infrastructure.Updates;

/// <summary>
/// <see cref="IUpdateService"/> backed by Velopack's <see cref="UpdateManager"/> against this
/// project's GitHub Releases. Per-user install ⇒ no UAC. Degrades to a no-op when the app is not
/// Velopack-managed (dev/unpackaged): the manager is built lazily and reports
/// <see cref="UpdateError.NotInstalled"/> rather than throwing on construction.
/// </summary>
public sealed class VelopackUpdateService : IUpdateService
{
    private readonly ILogger _log = Log.ForContext<VelopackUpdateService>();
    private readonly IElevationService _elevation;
    private readonly Lazy<UpdateManager?> _manager;
    private UpdateInfo? _pending;
    private bool _downloaded;

    public VelopackUpdateService(IElevationService elevation)
    {
        ArgumentNullException.ThrowIfNull(elevation);
        _elevation = elevation;
        _manager = new Lazy<UpdateManager?>(CreateManager);
    }

    /// <inheritdoc />
    public bool IsUpdateSupported => _manager.Value is { IsInstalled: true };

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var manager = _manager.Value;
        if (manager is null || !manager.IsInstalled)
        {
            _log.Debug("Update check skipped: app is not Velopack-installed (dev/unpackaged).");
            return UpdateCheckResult.Failed(UpdateError.NotInstalled);
        }

        try
        {
            // UpdateManager.CheckForUpdatesAsync has no CancellationToken overload; wrapping it in
            // Task.Run makes the wait cancellable (the underlying HTTP request still completes).
            _pending = await Task.Run(() => manager.CheckForUpdatesAsync(), cancellationToken).ConfigureAwait(false);
            _downloaded = false;

            if (_pending is null)
            {
                _log.Information("No update available (current {Version}).", manager.CurrentVersion);
                return UpdateCheckResult.UpToDate;
            }

            var version = _pending.TargetFullRelease.Version.ToString();
            _log.Information("Update available: {Version}.", version);
            return UpdateCheckResult.Available(version);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // the user closed the dialog
        }
        catch (Exception ex)
        {
            // Includes HttpClient timeouts (TaskCanceledException without our token being cancelled).
            var error = UpdateErrorMapper.Map(ex);
            _log.Warning(ex, "Update check failed -> {Error}.", error);
            return UpdateCheckResult.Failed(error);
        }
    }

    /// <inheritdoc />
    public async Task<UpdateError> DownloadAsync(IProgress<int>? progress, CancellationToken cancellationToken = default)
    {
        var manager = _manager.Value;
        if (manager is null || !manager.IsInstalled)
        {
            return UpdateError.NotInstalled;
        }

        if (_pending is null)
        {
            return UpdateError.Unknown; // CheckForUpdatesAsync must have found an update first.
        }

        if (_downloaded)
        {
            return UpdateError.None;
        }

        try
        {
            // Bridge Velopack's Action<int> callback to IProgress<int> (which marshals to the UI thread).
            await manager.DownloadUpdatesAsync(_pending, p => progress?.Report(p), cancellationToken).ConfigureAwait(false);
            _downloaded = true;
            _log.Information("Update {Version} downloaded.", _pending.TargetFullRelease.Version);
            return UpdateError.None;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = UpdateErrorMapper.Map(ex);
            _log.Error(ex, "Update download failed -> {Error}.", error);
            return error;
        }
    }

    /// <inheritdoc />
    public async Task<UpdateError> ApplyAndRestartAsync(CancellationToken cancellationToken = default)
    {
        var manager = _manager.Value;
        if (manager is null || !manager.IsInstalled)
        {
            return UpdateError.NotInstalled;
        }

        if (_pending is null || !_downloaded)
        {
            return UpdateError.Unknown;
        }

        try
        {
            // The elevated Elevator runs from the folder Velopack is about to replace and the
            // (non-elevated) updater can neither kill it nor overwrite its locked files.
            await _elevation.EndSessionAsync(cancellationToken).ConfigureAwait(false);

            _log.Information("Applying update {Version} and restarting.", _pending.TargetFullRelease.Version);
            manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
            return UpdateError.None; // Not reached: ApplyUpdatesAndRestart exits the process.
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = UpdateErrorMapper.Map(ex);
            _log.Error(ex, "Applying the update failed -> {Error}.", error);
            return error;
        }
    }

    private UpdateManager? CreateManager()
    {
        try
        {
            return new UpdateManager(new GithubSource(ProductLinks.Repository, accessToken: null, prerelease: false));
        }
        catch (InvalidOperationException ex)
        {
            // No VelopackLocator (the process was not launched as a Velopack-installed app).
            _log.Debug(ex, "Velopack UpdateManager unavailable; updates not supported in this context.");
            return null;
        }
    }
}
