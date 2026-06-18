using CommandForge.Application.Logging;
using CommandForge.Application.Ports;
using Serilog.Core;
using Serilog.Events;

namespace CommandForge.Infrastructure.Logging;

/// <summary><see cref="ILogLevelController"/> backed by the shared Serilog <see cref="LoggingLevelSwitch"/>.</summary>
public sealed class SerilogLevelController : ILogLevelController
{
    private readonly LoggingLevelSwitch _levelSwitch;

    public SerilogLevelController(LoggingLevelSwitch levelSwitch)
    {
        ArgumentNullException.ThrowIfNull(levelSwitch);
        _levelSwitch = levelSwitch;
    }

    /// <inheritdoc />
    public LogLevel Current
    {
        get => ToLogLevel(_levelSwitch.MinimumLevel);
        set => _levelSwitch.MinimumLevel = ToSerilog(value);
    }

    private static LogEventLevel ToSerilog(LogLevel level) => level switch
    {
        LogLevel.Verbose => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Fatal => LogEventLevel.Fatal,
        _ => LogEventLevel.Information,
    };

    private static LogLevel ToLogLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LogLevel.Verbose,
        LogEventLevel.Debug => LogLevel.Debug,
        LogEventLevel.Information => LogLevel.Information,
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Error => LogLevel.Error,
        LogEventLevel.Fatal => LogLevel.Fatal,
        _ => LogLevel.Information,
    };
}
