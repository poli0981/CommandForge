using System.Text.Json;
using CommandForge.Application.Ports;
using CommandForge.Application.Settings;

namespace CommandForge.Infrastructure.Portability;

/// <summary><see cref="ISettingsFile"/> backed by System.Text.Json (used for import/export).</summary>
public sealed class JsonSettingsFile : ISettingsFile
{
    /// <inheritdoc />
    public void Write(string path, PortableSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var json = JsonSerializer.Serialize(settings, PortableSettingsJsonContext.Default.PortableSettings);
        File.WriteAllText(path, json);
    }

    /// <inheritdoc />
    public PortableSettings? Read(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize(json, PortableSettingsJsonContext.Default.PortableSettings);
        }
        catch (JsonException)
        {
            // Not a valid settings file — caller surfaces a friendly error.
            return null;
        }
    }
}
