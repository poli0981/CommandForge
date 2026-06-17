using CommandForge.Application.Ports;
using CommandForge.Application.Updates;
using Serilog;
using Velopack;
using Velopack.Sources;

namespace CommandForge.Infrastructure.Updates;

/// <summary>
/// <see cref="IUpdateService"/> backed by Velopack's <see cref="UpdateManager"/> against this
/// project's GitHub Releases. Per-user install ⇒ no UAC, so it never touches the elevation broker.
/// Degrades to a no-op when the app is not Velopack-managed (dev/unpackaged): the manager is built
/// lazily and reports <see cref="UpdateError.NotInstalled"/> rather than throwing on construction.
/// </summary>
public sealed class VelopackUpdateService : IUpdateService
{
    private const string RepoUrl = "https://github.com/poli0981/CommandForge";

    private readonly ILogger _log = Log.ForContext<VelopackUpdateService>();
    private readonly Lazy<UpdateManager?> _manager;
    private UpdateInfo? _pending;

    public VelopackUpdateService() => _manager = new Lazy<UpdateManager?>(CreateManager);

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

            if (_pending is null)
            {
                _log.Information("No update available (current {Version}).", manager.CurrentVersion);
                return UpdateCheckResult.UpToDate;
            }

            var version = _pending.TargetFullRelease.Version.ToString();
            _log.Information("Update available: {Version}.", version);
            return UpdateCheckResult.Available(version);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = UpdateErrorMapper.Map((ex as HttpRequestException)?.StatusCode, ex);
            _log.Warning(ex, "Update check failed -> {Error}.", error);
            return UpdateCheckResult.Failed(error);
        }
    }

    /// <inheritdoc />
    public async Task<UpdateError> DownloadAndApplyAsync(IProgress<int>? progress, CancellationToken cancellationToken = default)
    {
        var manager = _manager.Value;
        if (manager is null || !manager.IsInstalled)
        {
            return UpdateError.NotInstalled;
        }

        if (_pending is null)
        {
            // CheckForUpdatesAsync must have succeeded with an update first.
            return UpdateError.Unknown;
        }

        try
        {
            // Bridge Velopack's Action<int> callback to IProgress<int> (which marshals to the UI thread).
            await manager.DownloadUpdatesAsync(_pending, p => progress?.Report(p), cancellationToken).ConfigureAwait(false);
            _log.Information("Update downloaded; applying and restarting.");
            manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
            return UpdateError.None; // Not reached: ApplyUpdatesAndRestart exits the process.
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = UpdateErrorMapper.Map((ex as HttpRequestException)?.StatusCode, ex);
            _log.Error(ex, "Update download/apply failed -> {Error}.", error);
            return error;
        }
    }

    private UpdateManager? CreateManager()
    {
        try
        {
            return new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
        }
        catch (InvalidOperationException ex)
        {
            // No VelopackLocator (the process was not launched as a Velopack-installed app).
            _log.Debug(ex, "Velopack UpdateManager unavailable; updates not supported in this context.");
            return null;
        }
    }
}
