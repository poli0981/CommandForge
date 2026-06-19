using CommandForge.Domain;

namespace CommandForge.Application.Recipes;

/// <summary>Pure decision logic for running a recipe sequentially.</summary>
public static class RecipeFlow
{
    /// <summary>
    /// Whether the recipe must stop after a step that produced this result: a failure, a
    /// cancellation, or a reboot-required signal (remaining steps may depend on the restart, so
    /// the safe default is to stop and let the user reboot).
    /// </summary>
    public static bool ShouldStop(ExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Cancelled || !result.Success || result.RequiresRestart;
    }
}
