namespace CommandForge.Domain;

/// <summary>
/// A named, ordered sequence of catalog command ids run together as one "recipe" (macro),
/// e.g. CheckHealth → ScanHealth → RestoreHealth → SFC.
/// </summary>
public sealed record Recipe
{
    /// <summary>User-visible recipe name (also the identity — matched case-insensitively).</summary>
    public required string Name { get; init; }

    /// <summary>The catalog command ids to run, in order.</summary>
    public required IReadOnlyList<string> CommandIds { get; init; }
}
