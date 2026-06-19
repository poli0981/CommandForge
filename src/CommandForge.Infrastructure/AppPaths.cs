namespace CommandForge.Infrastructure;

/// <summary>
/// Resolves the application's local data paths under <c>%AppData%\CommandForge</c>.
/// Portable-mode relocation is deferred to a later phase.
/// </summary>
public static class AppPaths
{
    /// <summary>Folder name used under %AppData% and %LocalAppData%.</summary>
    public const string AppFolderName = "CommandForge";

    /// <summary><c>%AppData%\CommandForge</c> — config and logs root.</summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppFolderName);

    /// <summary><c>%AppData%\CommandForge\logs</c>.</summary>
    public static string LogsDirectory { get; } = Path.Combine(DataDirectory, "logs");

    /// <summary><c>%AppData%\CommandForge\config.json</c>.</summary>
    public static string ConfigFilePath { get; } = Path.Combine(DataDirectory, "config.json");

    /// <summary><c>%AppData%\CommandForge\history.json</c> — persisted execution history.</summary>
    public static string HistoryFilePath { get; } = Path.Combine(DataDirectory, "history.json");

    /// <summary><c>%AppData%\CommandForge\profiles.json</c> — named settings/favorites profiles.</summary>
    public static string ProfilesFilePath { get; } = Path.Combine(DataDirectory, "profiles.json");

    /// <summary>Ensures the data and logs directories exist.</summary>
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
