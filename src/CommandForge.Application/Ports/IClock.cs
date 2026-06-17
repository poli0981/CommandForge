namespace CommandForge.Application.Ports;

/// <summary>
/// Abstraction over the system clock, for testability.
/// </summary>
public interface IClock
{
    /// <summary>The current instant.</summary>
    public DateTimeOffset Now { get; }
}
