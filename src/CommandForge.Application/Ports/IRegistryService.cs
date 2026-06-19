using CommandForge.Domain;

namespace CommandForge.Application.Ports;

/// <summary>
/// Reads registry values for read-only before/after comparison. CommandForge never writes the
/// registry through this service — changes happen only via vetted catalog commands.
/// </summary>
public interface IRegistryService
{
    /// <summary>Reads the current value as a string, or null if the key/value does not exist.</summary>
    public string? Read(RegistryValueRef reference);
}
