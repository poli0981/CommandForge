namespace CommandForge.Application.Ports;

/// <summary>
/// Provides a read-only snapshot of basic system status for the Home dashboard. Implementations
/// must not spawn processes, require elevation, or send anything off the machine.
/// </summary>
public interface ISystemInfoService
{
    /// <summary>Reads the current system status. Cheap, synchronous and read-only.</summary>
    public SystemStatus GetStatus();
}
