using CommandForge.Application.Ports;
using CommandForge.Application.Settings;
using CommandForge.Infrastructure.Persistence;

namespace CommandForge.Infrastructure.Portability;

/// <summary><see cref="ISettingsFile"/> backed by System.Text.Json (used for import/export).</summary>
public sealed class JsonSettingsFile : ISettingsFile
{
    /// <inheritdoc />
    public void Write(string path, PortableSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Throws on I/O errors: the user picked this path, so the caller reports the failure.
        JsonFile.Write(path, settings, PortableSettingsJsonContext.Default.PortableSettings);
    }

    /// <inheritdoc />
    public PortableSettings? Read(string path)
    {
        // Missing, unreadable or invalid files all come back as null; the caller shows a friendly error.
        // A user-chosen import file is never renamed, even when it is not valid JSON.
        return JsonFile.Read(path, PortableSettingsJsonContext.Default.PortableSettings, keepCorruptCopy: false);
    }
}
