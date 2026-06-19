using CommandForge.Application.Settings;

namespace CommandForge.Application.Ports;

/// <summary>
/// Stores named profiles — bundles of portable settings + favorites by context
/// (e.g. "Gaming PC", "Office"). Persisted locally (golden rule: no telemetry).
/// </summary>
public interface IProfileService
{
    /// <summary>Profile names, in stored order.</summary>
    public IReadOnlyList<string> GetNames();

    /// <summary>The settings stored under <paramref name="name"/>, or null if none.</summary>
    public PortableSettings? Get(string name);

    /// <summary>Creates or overwrites the profile <paramref name="name"/> and persists.</summary>
    public void Save(string name, PortableSettings settings);

    /// <summary>Deletes the profile <paramref name="name"/> (no-op if absent) and persists.</summary>
    public void Delete(string name);
}
