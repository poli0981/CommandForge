namespace CommandForge.Domain;

/// <summary>
/// How to decide whether a command requires a system restart.
/// </summary>
public enum RestartPolicy
{
    /// <summary>Never requires a restart.</summary>
    No,

    /// <summary>Always requires a restart (e.g. scheduled at boot).</summary>
    Always,

    /// <summary>Requires a restart when the process exits with code 3010 (ERROR_SUCCESS_REBOOT_REQUIRED).</summary>
    FromExitCode,

    /// <summary>Requires a restart when the output matches <see cref="CommandDefinition.RestartRegex"/>.</summary>
    FromOutputRegex,
}
