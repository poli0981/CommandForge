namespace CommandForge.Domain;

/// <summary>
/// A user-defined command. These are NOT part of the vetted, embedded catalog and ALWAYS run
/// without elevation (asInvoker) — see golden rule #1. Stored separately from the catalog.
/// </summary>
public sealed record UserCommand
{
    /// <summary>Stable local id (generated when the command is created).</summary>
    public required string Id { get; init; }

    /// <summary>User-visible name.</summary>
    public required string Name { get; init; }

    /// <summary>Executable to run (e.g. <c>cmd</c>, <c>powershell</c>, or a full path).</summary>
    public required string Executable { get; init; }

    /// <summary>Command-line arguments (optional).</summary>
    public string Arguments { get; init; } = "";
}
