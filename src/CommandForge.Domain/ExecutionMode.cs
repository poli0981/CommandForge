namespace CommandForge.Domain;

/// <summary>How a command is run.</summary>
public enum ExecutionMode
{
    /// <summary>Console command: redirect stdout/stderr and stream output (e.g. ipconfig, systeminfo).</summary>
    Capture,

    /// <summary>Launches a GUI / shell target and returns immediately; no output to capture (e.g. winver, cleanmgr).</summary>
    Launch,
}
