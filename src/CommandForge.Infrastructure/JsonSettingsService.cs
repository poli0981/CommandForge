using System.Text.Json;
using CommandForge.Application.Ports;

namespace CommandForge.Infrastructure;

/// <summary>
/// <see cref="ISettingsService"/> backed by a local <c>config.json</c>. All data stays on
/// the machine (see Privacy policy / golden rule "no telemetry").
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _configPath;
    private SettingsModel _model;

    /// <summary>Creates a service backed by the default <see cref="AppPaths.ConfigFilePath"/>.</summary>
    public JsonSettingsService()
        : this(AppPaths.ConfigFilePath)
    {
    }

    /// <summary>Creates a service backed by an explicit config path (used in tests).</summary>
    public JsonSettingsService(string configPath)
    {
        _configPath = configPath;
        _model = Load(configPath);
    }

    /// <inheritdoc />
    public string? AcceptedTermsVersion
    {
        get => _model.AcceptedTermsVersion;
        set => _model = _model with { AcceptedTermsVersion = value };
    }

    /// <inheritdoc />
    public void Save()
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_model, SerializerOptions);
        File.WriteAllText(_configPath, json);
    }

    private static SettingsModel Load(string path)
    {
        if (!File.Exists(path))
        {
            return new SettingsModel();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
        }
        catch (JsonException)
        {
            // Corrupt config: fall back to defaults rather than crashing on startup.
            return new SettingsModel();
        }
    }

    private sealed record SettingsModel
    {
        public string? AcceptedTermsVersion { get; init; }
    }
}
