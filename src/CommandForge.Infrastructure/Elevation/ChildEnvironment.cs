namespace CommandForge.Infrastructure.Elevation;

/// <summary>
/// Builds the environment for processes the elevated Elevator starts. The Elevator inherits the
/// user's environment block, and a standard user can define per-user variables (HKCU\Environment)
/// that redirect what an elevated child loads or touches: <c>COR_PROFILER</c>/<c>CORECLR_*</c>
/// inject a DLL into .NET processes such as PowerShell, <c>PSModulePath</c> makes PowerShell
/// auto-load modules from the user's Documents folder, and <c>windir</c>/<c>SystemRoot</c> retarget
/// the recursive deletes some cleanup commands perform. So the child gets an allow-list: a few
/// harmless pass-through values plus system paths re-derived from trusted sources.
/// </summary>
internal static class ChildEnvironment
{
    // Copied from the Elevator's own environment when present (identity / hardware / user folders).
    private static readonly string[] PassThrough =
    [
        "APPDATA", "COMPUTERNAME", "HOMEDRIVE", "HOMEPATH", "LOCALAPPDATA", "LOGONSERVER",
        "NUMBER_OF_PROCESSORS", "OS", "PROCESSOR_ARCHITECTURE", "PROCESSOR_IDENTIFIER",
        "PROCESSOR_LEVEL", "PROCESSOR_REVISION", "SESSIONNAME", "TEMP", "TMP", "USERDNSDOMAIN",
        "USERDOMAIN", "USERDOMAIN_ROAMINGPROFILE", "USERNAME",
    ];

    private const string SessionEnvironmentKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

    /// <summary>Replaces the contents of <paramref name="environment"/> with the sanitized set.</summary>
    public static void Apply(IDictionary<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var kept = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in PassThrough)
        {
            var match = environment.FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase));
            if (match.Key is not null && !string.IsNullOrEmpty(match.Value))
            {
                kept[name] = match.Value;
            }
        }

        environment.Clear();
        foreach (var (name, value) in kept)
        {
            environment[name] = value;
        }

        foreach (var (name, value) in TrustedValues())
        {
            environment[name] = value;
        }
    }

    /// <summary>System variables with values from the kernel, known folders and HKLM.</summary>
    internal static IReadOnlyDictionary<string, string> TrustedValues()
    {
        var system32 = SystemPaths.System32;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SystemRoot"] = SystemPaths.WindowsDirectory,
            ["windir"] = SystemPaths.WindowsDirectory,
            ["SystemDrive"] = SystemPaths.SystemDrive,
            ["ComSpec"] = Path.Combine(system32, "cmd.exe"),
            ["USERPROFILE"] = SystemPaths.UserProfile,
            ["ProgramData"] = SystemPaths.ProgramData,
            ["ALLUSERSPROFILE"] = SystemPaths.ProgramData,
            ["PUBLIC"] = SystemPaths.Public,
            ["ProgramFiles"] = SystemPaths.ProgramFiles,
            ["ProgramW6432"] = SystemPaths.ProgramFiles,
            ["ProgramFiles(x86)"] = SystemPaths.ProgramFilesX86,
            ["CommonProgramFiles"] = SystemPaths.CommonProgramFiles,
            ["CommonProgramW6432"] = SystemPaths.CommonProgramFiles,
            ["CommonProgramFiles(x86)"] = SystemPaths.CommonProgramFilesX86,

            // Only $PSHOME\Modules: Windows PowerShell 5.1 re-inserts the per-user module folder when
            // this variable is missing or equals the machine value, but keeps a narrower one as-is.
            ["PSModulePath"] = Path.Combine(system32, "WindowsPowerShell", "v1.0", "Modules"),

            ["Path"] = MachinePath(),
            ["PATHEXT"] = SystemPaths.ReadMachineValue(SessionEnvironmentKey, "PATHEXT") ?? ".COM;.EXE;.BAT;.CMD;.VBS;.JS;.WSF;.WSH;.MSC",
        };

        return values;
    }

    /// <summary>The machine PATH from HKLM (no per-user directories), expanded with trusted values only.</summary>
    private static string MachinePath()
    {
        var fallback = string.Join(';', SystemPaths.System32, SystemPaths.WindowsDirectory, Path.Combine(SystemPaths.System32, "Wbem"), Path.Combine(SystemPaths.System32, "WindowsPowerShell", "v1.0"));
        if (SystemPaths.ReadMachineRawValue(SessionEnvironmentKey, "Path") is not { } raw)
        {
            return fallback;
        }

        var entries = raw
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SystemPaths.ExpandTrusted)
            .Where(entry => entry is not null && Path.IsPathFullyQualified(entry))
            .Cast<string>()
            .ToList();
        return entries.Count == 0 ? fallback : string.Join(';', entries);
    }
}
