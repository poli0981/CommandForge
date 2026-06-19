using System.Text.Json;
using CommandForge.Application.Ports;
using CommandForge.Application.Settings;

namespace CommandForge.Infrastructure.Portability;

/// <summary>
/// <see cref="IProfileService"/> backed by a local <c>profiles.json</c>. All data stays on the
/// machine (golden rule: no telemetry). Profile names are matched case-insensitively.
/// </summary>
public sealed class JsonProfileService : IProfileService
{
    private readonly string _path;
    private List<ProfileEntry> _profiles;

    /// <summary>Creates a service backed by the default <see cref="AppPaths.ProfilesFilePath"/>.</summary>
    public JsonProfileService()
        : this(AppPaths.ProfilesFilePath)
    {
    }

    /// <summary>Creates a service backed by an explicit path (used in tests).</summary>
    public JsonProfileService(string path)
    {
        _path = path;
        _profiles = Load(path);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetNames() => _profiles.Select(p => p.Name).ToList();

    /// <inheritdoc />
    public PortableSettings? Get(string name)
        => _profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))?.Settings;

    /// <inheritdoc />
    public void Save(string name, PortableSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(settings);

        _profiles.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        _profiles.Add(new ProfileEntry { Name = name, Settings = settings });
        SaveFile();
    }

    /// <inheritdoc />
    public void Delete(string name)
    {
        if (_profiles.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            SaveFile();
        }
    }

    private void SaveFile()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var file = new ProfilesFile { Profiles = _profiles };
        var json = JsonSerializer.Serialize(file, PortableSettingsJsonContext.Default.ProfilesFile);
        File.WriteAllText(_path, json);
    }

    private static List<ProfileEntry> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize(json, PortableSettingsJsonContext.Default.ProfilesFile);
            return file?.Profiles.ToList() ?? [];
        }
        catch (JsonException)
        {
            // Corrupt profiles file: start empty rather than crashing on startup.
            return [];
        }
    }
}
