using CommandForge.Application.Logging;

namespace CommandForge.Application.Ports;

/// <summary>Gets/sets the runtime minimum log level (backed by a Serilog level switch in Infrastructure).</summary>
public interface ILogLevelController
{
    /// <summary>The current minimum level captured by the logging pipeline.</summary>
    public LogLevel Current { get; set; }
}
