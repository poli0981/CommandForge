using CommandForge.Application.Ports;
using CommandForge.Application.Settings;
using CommandForge.Infrastructure.Persistence;

namespace CommandForge.Infrastructure.Portability;

/// <summary>
/// <see cref="IProfileService"/> backed by a local <c>profiles.json</c>. All data stays on the
/// machine (golden rule: no telemetry). Profile names are matched case-insensitively.
/// </summary>
public sealed class JsonProfileService : IProfileService
{
    private readonly string _path;
    private readonly Lock _gate = new();
    private readonly List<ProfileEntry> _profiles;

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
    public IReadOnlyList<string> GetNames()
    {
        lock (_gate)
        {
            return _profiles.Select(p => p.Name).ToList();
        }
    }

    /// <inheritdoc />
    public PortableSettings? Get(string name)
    {
        lock (_gate)
        {
            return _profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))?.Settings;
        }
    }

    /// <inheritdoc />
    public void Save(string name, PortableSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(settings);

        lock (_gate)
        {
            _profiles.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            _profiles.Add(new ProfileEntry { Name = name, Settings = settings });
            SaveFile();
        }
    }

    /// <inheritdoc />
    public void Delete(string name)
    {
        lock (_gate)
        {
            if (_profiles.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                SaveFile();
            }
        }
    }

    private void SaveFile()
        => JsonFile.TryWrite(_path, new ProfilesFile { Profiles = _profiles }, PortableSettingsJsonContext.Default.ProfilesFile);

    private static List<ProfileEntry> Load(string path)
        => JsonFile.Read(path, PortableSettingsJsonContext.Default.ProfilesFile)?.Profiles?.Where(item => item is not null).ToList() ?? [];
}
