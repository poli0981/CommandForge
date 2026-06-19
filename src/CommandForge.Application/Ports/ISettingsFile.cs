using CommandForge.Application.Settings;

namespace CommandForge.Application.Ports;

/// <summary>
/// Reads/writes portable settings to a user-chosen file (import/export between machines).
/// </summary>
public interface ISettingsFile
{
    /// <summary>Writes <paramref name="settings"/> to <paramref name="path"/> as JSON.</summary>
    public void Write(string path, PortableSettings settings);

    /// <summary>Reads portable settings from <paramref name="path"/>; null if missing or invalid.</summary>
    public PortableSettings? Read(string path);
}
