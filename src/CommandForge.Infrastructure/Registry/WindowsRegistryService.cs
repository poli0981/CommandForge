using CommandForge.Application.Ports;
using CommandForge.Domain;
using Microsoft.Win32;

namespace CommandForge.Infrastructure.Registry;

/// <summary>
/// <see cref="IRegistryService"/> over <see cref="Microsoft.Win32.Registry"/> — read-only.
/// Unknown hives, missing keys/values, or access errors all read as <c>null</c>.
/// </summary>
public sealed class WindowsRegistryService : IRegistryService
{
    /// <inheritdoc />
    public string? Read(RegistryValueRef reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var (hive, subKey) = Split(reference.Path);
        if (hive is null)
        {
            return null;
        }

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive.Value, RegistryView.Default);
            using var key = baseKey.OpenSubKey(subKey);
            return key?.GetValue(reference.Name)?.ToString();
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }

    private static (RegistryHive? Hive, string SubKey) Split(string path)
    {
        var separator = path.IndexOf('\\', StringComparison.Ordinal);
        var prefix = separator < 0 ? path : path[..separator];
        var subKey = separator < 0 ? string.Empty : path[(separator + 1)..];

        RegistryHive? hive = prefix.ToUpperInvariant() switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKU" or "HKEY_USERS" => RegistryHive.Users,
            "HKCC" or "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => null,
        };

        return (hive, subKey);
    }
}
