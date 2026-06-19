namespace CommandForge.Domain;

/// <summary>
/// A record of one finished command execution, persisted for the execution-history view.
/// </summary>
public sealed record ExecutionRecord
{
    /// <summary>The catalog id of the command that ran.</summary>
    public required string CommandId { get; init; }

    /// <summary>When the run finished (clock instant, with offset).</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>The process exit code.</summary>
    public required int ExitCode { get; init; }

    /// <summary>Whether the run was considered successful (exit 0 or the reboot-required code).</summary>
    public required bool Success { get; init; }

    /// <summary>Wall-clock duration in milliseconds.</summary>
    public required long DurationMs { get; init; }

    /// <summary>Whether the run signalled that a restart is required (e.g. exit code 3010).</summary>
    public bool RequiresRestart { get; init; }
}
