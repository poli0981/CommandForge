using Velopack;

namespace CommandForge.Infrastructure.Updates;

/// <summary>
/// Runs Velopack's install/update hooks. Must be invoked as early as possible at process start
/// (before the UI and host) — Velopack handles its <c>--veloapp-*</c> hooks here and may exit the
/// process. Keeping the Velopack dependency in Infrastructure keeps it out of the WPF layer.
/// </summary>
public static class VelopackBootstrap
{
    /// <summary>Runs the Velopack lifecycle hooks. Call this as the first line of app startup.</summary>
    public static void Run() => VelopackApp.Build().Run();
}
