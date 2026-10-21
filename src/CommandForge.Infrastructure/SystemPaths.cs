using Microsoft.Win32;

namespace CommandForge.Infrastructure;

/// <summary>
/// Machine paths resolved from sources a standard user cannot redefine — the kernel-reported system
/// directory, known folders and HKLM — never from environment variables. A per-user variable such
/// as <c>windir</c> or <c>SystemRoot</c> in HKCU\Environment overrides the system value in every
/// process the user starts, elevated ones included, so commands running as administrator must not
/// build paths from <c>%SystemRoot%</c>.
/// </summary>
public static class SystemPaths
{
    private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion";
    private const string ProfileListKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";

    /// <summary><c>C:\Windows\System32</c> (GetSystemDirectory — reported by the OS, not the environment).</summary>
    public static string System32 { get; } = Environment.SystemDirectory;

    /// <summary><c>C:\Windows</c>.</summary>
    public static string WindowsDirectory { get; } = Path.GetDirectoryName(Environment.SystemDirectory) ?? @"C:\Windows";

    /// <summary><c>C:</c> — the drive Windows is installed on.</summary>
    public static string SystemDrive { get; } = (Path.GetPathRoot(WindowsDirectory) ?? @"C:\").TrimEnd('\\');

    /// <summary>The user's profile folder (known folder, backed by the HKLM profile list).</summary>
    public static string UserProfile { get; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>The user's local application data folder.</summary>
    public static string LocalAppData { get; } = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private static string DriveRoot => SystemDrive + Path.DirectorySeparatorChar;

    /// <summary><c>C:\Program Files</c> (native).</summary>
    public static string ProgramFiles => ReadMachineValue(CurrentVersionKey, "ProgramW6432Dir") ?? ReadMachineValue(CurrentVersionKey, "ProgramFilesDir") ?? Path.Combine(DriveRoot, "Program Files");

    /// <summary><c>C:\Program Files (x86)</c>.</summary>
    public static string ProgramFilesX86 => ReadMachineValue(CurrentVersionKey, "ProgramFilesDir (x86)") ?? ProgramFiles;

    /// <summary><c>C:\Program Files\Common Files</c>.</summary>
    public static string CommonProgramFiles => ReadMachineValue(CurrentVersionKey, "CommonW6432Dir") ?? ReadMachineValue(CurrentVersionKey, "CommonFilesDir") ?? Path.Combine(ProgramFiles, "Common Files");

    /// <summary><c>C:\Program Files (x86)\Common Files</c>.</summary>
    public static string CommonProgramFilesX86 => ReadMachineValue(CurrentVersionKey, "CommonFilesDir (x86)") ?? CommonProgramFiles;

    /// <summary><c>C:\ProgramData</c>.</summary>
    public static string ProgramData => ReadMachineValue(ProfileListKey, "ProgramData") ?? Path.Combine(DriveRoot, "ProgramData");

    /// <summary><c>C:\Users\Public</c>.</summary>
    public static string Public => ReadMachineValue(ProfileListKey, "Public") ?? Path.Combine(DriveRoot, "Users", "Public");

    /// <summary>
    /// Expands the handful of system variables that HKLM values use (<c>%SystemRoot%</c>,
    /// <c>%SystemDrive%</c>, …) with the trusted values above. Returns null if anything else remains.
    /// </summary>
    public static string? ExpandTrusted(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var expanded = value
            .Replace("%SystemRoot%", WindowsDirectory, StringComparison.OrdinalIgnoreCase)
            .Replace("%windir%", WindowsDirectory, StringComparison.OrdinalIgnoreCase)
            .Replace("%SystemDrive%", SystemDrive, StringComparison.OrdinalIgnoreCase);
        return expanded.Contains('%', StringComparison.Ordinal) ? null : expanded;
    }

    /// <summary>Reads a string value under HKLM (64-bit view), expanding only trusted variables.</summary>
    internal static string? ReadMachineValue(string keyPath, string valueName)
        => ReadMachineRawValue(keyPath, valueName) is { } raw ? ExpandTrusted(raw) : null;

    /// <summary>Reads a string value under HKLM (64-bit view) without expanding anything.</summary>
    internal static string? ReadMachineRawValue(string keyPath, string valueName)
    {
        try
        {
            using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hive.OpenSubKey(keyPath);
            return key?.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }
}
