namespace CommandForge.Domain;

/// <summary>
/// Reference to a single registry value that a command may modify, used for read-only
/// before/after comparison. Reading only — CommandForge never writes the registry directly.
/// </summary>
public sealed record RegistryValueRef
{
    /// <summary>Full key path including the hive prefix, e.g. <c>HKCU\Software\…\Advanced</c>.</summary>
    public required string Path { get; init; }

    /// <summary>Value name under the key (empty string means the key's default value).</summary>
    public required string Name { get; init; }
}
