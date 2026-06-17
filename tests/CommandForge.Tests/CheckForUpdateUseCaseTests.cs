using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;

namespace CommandForge.Tests;

/// <summary>
/// Verifies <see cref="CheckForUpdateUseCase"/> passes results/errors through and surfaces support,
/// using a hand-rolled fake (no mocking framework, matching the repo's fake style).
/// </summary>
public sealed class CheckForUpdateUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsServiceResult()
    {
        var fake = new FakeUpdateService { Result = UpdateCheckResult.Available("1.2.3") };
        var useCase = new CheckForUpdateUseCase(fake);

        var result = await useCase.ExecuteAsync();

        Assert.True(result.HasUpdate);
        Assert.Equal("1.2.3", result.NewVersion);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesError()
    {
        var fake = new FakeUpdateService { Result = UpdateCheckResult.Failed(UpdateError.RateLimited) };
        var useCase = new CheckForUpdateUseCase(fake);

        var result = await useCase.ExecuteAsync();

        Assert.False(result.HasUpdate);
        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateError.RateLimited, result.Error);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSupported_ReflectsService(bool supported)
    {
        var useCase = new CheckForUpdateUseCase(new FakeUpdateService { IsUpdateSupported = supported });
        Assert.Equal(supported, useCase.IsSupported);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_PropagatesError()
    {
        var fake = new FakeUpdateService { DownloadResult = UpdateError.Offline };
        var useCase = new CheckForUpdateUseCase(fake);

        var error = await useCase.DownloadAndApplyAsync(progress: null);

        Assert.Equal(UpdateError.Offline, error);
    }

    private sealed class FakeUpdateService : IUpdateService
    {
        public bool IsUpdateSupported { get; init; } = true;

        public UpdateCheckResult Result { get; init; } = UpdateCheckResult.UpToDate;

        public UpdateError DownloadResult { get; init; } = UpdateError.None;

        public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result);

        public Task<UpdateError> DownloadAndApplyAsync(IProgress<int>? progress, CancellationToken cancellationToken = default)
            => Task.FromResult(DownloadResult);
    }
}
