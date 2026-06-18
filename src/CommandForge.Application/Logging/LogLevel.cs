namespace CommandForge.Application.Logging;

/// <summary>Severity of a log entry. Mirrors Serilog's levels but keeps Application Serilog-free.</summary>
public enum LogLevel
{
    Verbose,
    Debug,
    Information,
    Warning,
    Error,
    Fatal,
}
