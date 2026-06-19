using CommandForge.Application.Recipes;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>Tests the pure recipe stop-on-error/restart/cancel decision.</summary>
public sealed class RecipeFlowTests
{
    private static ExecutionResult Result(bool success = true, bool cancelled = false, bool restart = false)
        => new()
        {
            ExitCode = success ? 0 : 1,
            Success = success,
            Duration = TimeSpan.Zero,
            Cancelled = cancelled,
            RequiresRestart = restart,
        };

    [Fact]
    public void ShouldStop_False_OnPlainSuccess()
        => Assert.False(RecipeFlow.ShouldStop(Result()));

    [Fact]
    public void ShouldStop_True_OnFailure()
        => Assert.True(RecipeFlow.ShouldStop(Result(success: false)));

    [Fact]
    public void ShouldStop_True_OnCancelled()
        => Assert.True(RecipeFlow.ShouldStop(Result(cancelled: true)));

    [Fact]
    public void ShouldStop_True_OnRestartRequired()
        => Assert.True(RecipeFlow.ShouldStop(Result(restart: true))); // 3010: success-with-reboot, still stop
}
