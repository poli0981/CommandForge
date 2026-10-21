using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Wpf.Resources;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Tests;

/// <summary>Tests the update dialog's states: it never sticks on "Checking" and never restarts mid-command.</summary>
public sealed class UpdateViewModelTests
{
    private readonly FakeUpdates _updates = new();

    [Fact]
    public async Task Check_UnexpectedFailure_ShowsAnError()
    {
        _updates.Check = _ => throw new InvalidOperationException("boom");
        var viewModel = Create();

        await viewModel.CheckAsync();

        Assert.True(viewModel.IsError);
        Assert.False(viewModel.CheckSucceeded);
    }

    [Fact]
    public async Task Check_NoUpdate_IsUpToDate()
    {
        _updates.Check = _ => Task.FromResult(UpdateCheckResult.UpToDate);
        var viewModel = Create();

        await viewModel.CheckAsync();

        Assert.True(viewModel.IsUpToDate);
        Assert.True(viewModel.CheckSucceeded);
    }

    [Fact]
    public async Task Check_RateLimited_ExplainsIt()
    {
        _updates.Check = _ => Task.FromResult(UpdateCheckResult.Failed(UpdateError.RateLimited));
        var viewModel = Create();

        await viewModel.CheckAsync();

        Assert.True(viewModel.IsError);
        Assert.Equal(Strings.Get("Update_Error_RateLimited"), viewModel.Message);
    }

    [Fact]
    public async Task Download_WhileACommandRuns_IsRefused()
    {
        var viewModel = Create(commandRunning: () => true);
        viewModel.SeedAvailable("2.0.0");

        await viewModel.DownloadAndRestartCommand.ExecuteAsync(null);

        Assert.Equal(0, _updates.Downloads);
        Assert.True(viewModel.IsAvailable);
        Assert.Equal(Strings.Get("Update_Error_CommandRunning"), viewModel.Message);
    }

    [Fact]
    public async Task CommandStartedDuringDownload_WaitsBeforeRestarting()
    {
        var running = false;
        _updates.Download = _ =>
        {
            running = true; // the user started a command while the download was in progress
            return Task.FromResult(UpdateError.None);
        };
        var viewModel = Create(commandRunning: () => running);
        viewModel.SeedAvailable("2.0.0");

        await viewModel.DownloadAndRestartCommand.ExecuteAsync(null);

        Assert.Equal(0, _updates.Applies);
        Assert.True(viewModel.IsAvailable);
        Assert.Equal(Strings.Get("Update_ReadyAfterCommand"), viewModel.Message);
    }

    [Fact(Timeout = 30_000)]
    public async Task ClosingTheWindow_CancelsTheDownloadAndNeverApplies()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _updates.Download = async token =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
            return UpdateError.None;
        };
        var viewModel = Create();
        var closed = false;
        viewModel.CloseRequested += () => closed = true;
        viewModel.SeedAvailable("2.0.0");

        var download = viewModel.DownloadAndRestartCommand.ExecuteAsync(null);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        viewModel.CancelPending(); // the title-bar X
        await download;

        Assert.True(closed);
        Assert.Equal(0, _updates.Applies);
    }

    [Fact]
    public async Task ApplyThatDoesNotRestart_ShowsWhy()
    {
        _updates.Apply = _ => Task.FromResult(UpdateError.NotInstalled);
        var viewModel = Create();
        viewModel.SeedAvailable("2.0.0");

        await viewModel.DownloadAndRestartCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsError);
        Assert.Equal(Strings.Get("Update_Error_NotInstalled"), viewModel.Message);
    }

    private UpdateViewModel Create(Func<bool>? commandRunning = null)
        => new(new CheckForUpdateUseCase(_updates)) { IsCommandRunning = commandRunning };

    private sealed class FakeUpdates : IUpdateService
    {
        public Func<CancellationToken, Task<UpdateCheckResult>> Check { get; set; } = _ => Task.FromResult(UpdateCheckResult.UpToDate);

        public Func<CancellationToken, Task<UpdateError>> Download { get; set; } = _ => Task.FromResult(UpdateError.None);

        public Func<CancellationToken, Task<UpdateError>> Apply { get; set; } = _ => Task.FromResult(UpdateError.None);

        public int Downloads { get; private set; }

        public int Applies { get; private set; }

        public bool IsUpdateSupported => true;

        public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default) => Check(cancellationToken);

        public Task<UpdateError> DownloadAsync(IProgress<int>? progress, CancellationToken cancellationToken = default)
        {
            Downloads++;
            return Download(cancellationToken);
        }

        public Task<UpdateError> ApplyAndRestartAsync(CancellationToken cancellationToken = default)
        {
            Applies++;
            return Apply(cancellationToken);
        }
    }
}
