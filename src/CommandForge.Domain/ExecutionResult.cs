namespace CommandForge.Domain;

/// <summary>
/// The outcome of running a command.
/// </summary>
public sealed record ExecutionResult
{
    /// <summary>Process exit code.</summary>
    public required int ExitCode { get; init; }

    /// <summary>Whether the command is considered successful.</summary>
    public required bool Success { get; init; }

    /// <summary>Wall-clock duration of the run.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether a system restart is required to finish (e.g. exit code 3010 or a regex match).
    /// </summary>
    public bool RequiresRestart { get; init; }

    /// <summary>Captured stdout/stderr lines, in order.</summary>
    public IReadOnlyList<string> OutputLines { get; init; } = [];
}
