namespace CommandForge.Domain;

/// <summary>
/// Risk level of a command. Drives confirmation UI and safety floors.
/// </summary>
public enum DangerLevel
{
    /// <summary>Read-only / informational, no risk.</summary>
    Safe,

    /// <summary>Changes the system but is recoverable / low risk.</summary>
    Caution,

    /// <summary>May cause data loss or instability; requires strict confirmation.</summary>
    Dangerous,
}
