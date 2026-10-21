namespace CommandForge.Domain;

/// <summary>
/// Risk level of a command. Drives confirmation UI and safety floors.
/// </summary>
public enum DangerLevel
{
    /// <summary>
    /// No risk of losing data: read-only queries, opening a tool or settings page, or a small
    /// preference change that is undone the same way it was made.
    /// </summary>
    Safe,

    /// <summary>
    /// Changes system state (services, registry, caches, boot-time checks); recoverable, but undoing
    /// it may take effort. Confirmed before running unless the user turned that off.
    /// </summary>
    Caution,

    /// <summary>
    /// May lose data, remove a rollback option or leave Windows unstable. Always needs a typed
    /// confirmation, which the user cannot turn off.
    /// </summary>
    Dangerous,
}
