namespace CommandForge.Application.Ports;

/// <summary>
/// Reads and persists local user settings (config.json). No data leaves the machine.
/// </summary>
public interface ISettingsService
{
    /// <summary>The terms version the user last accepted, or <see langword="null"/> if none.</summary>
    public string? AcceptedTermsVersion { get; set; }

    /// <summary>Whether to check GitHub Releases for updates on startup (default <see langword="true"/>). Local-only.</summary>
    public bool AutoCheckForUpdates { get; set; }

    /// <summary>Persists the current settings to disk.</summary>
    public void Save();
}
