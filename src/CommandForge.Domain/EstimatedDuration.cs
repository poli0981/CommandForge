namespace CommandForge.Domain;

/// <summary>
/// Rough estimate of how long a command takes to run. Used for UI hints only.
/// </summary>
public enum EstimatedDuration
{
    /// <summary>Seconds.</summary>
    Short,

    /// <summary>Up to a minute or two.</summary>
    Medium,

    /// <summary>Several minutes or more (e.g. DISM, SFC).</summary>
    Long,
}
