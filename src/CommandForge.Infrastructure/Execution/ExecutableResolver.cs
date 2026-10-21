namespace CommandForge.Infrastructure.Execution;

/// <summary>
/// Turns a catalog executable name ("dism", "powershell", "services.msc") into an absolute path in
/// the Windows system directories. CreateProcess otherwise searches the application's own folder
/// first — for a per-user install that folder is writable by the user, so a planted "reg.exe" next
/// to the (elevated) Elevator would run with administrator rights.
/// </summary>
internal static class ExecutableResolver
{
    private static readonly string[] ImplicitExtensions = [".exe", ".com", ".msc", ".cpl"];

    /// <summary>Returns the absolute path for <paramref name="executable"/>, or the input unchanged when it can't be resolved.</summary>
    public static string Resolve(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable) || Path.IsPathRooted(executable)
            || executable.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            return executable;
        }

        if (IsName(executable, "powershell"))
        {
            return Path.Combine(SystemPaths.System32, "WindowsPowerShell", "v1.0", "powershell.exe");
        }

        if (IsName(executable, "winget"))
        {
            // winget is an App Execution Alias in the user's WindowsApps folder, not a system binary.
            var alias = Path.Combine(SystemPaths.LocalAppData, "Microsoft", "WindowsApps", "winget.exe");
            return File.Exists(alias) ? alias : executable;
        }

        var candidates = Path.HasExtension(executable)
            ? [executable]
            : ImplicitExtensions.Select(extension => executable + extension).ToArray();

        foreach (var directory in new[] { SystemPaths.System32, SystemPaths.WindowsDirectory })
        {
            foreach (var candidate in candidates)
            {
                var path = Path.Combine(directory, candidate);
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        return executable;
    }

    private static bool IsName(string executable, string name)
        => string.Equals(executable, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(executable, name + ".exe", StringComparison.OrdinalIgnoreCase);
}
