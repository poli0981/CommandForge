namespace CommandForge.Wpf.Services;

/// <summary>Opens links/folders and restarts Windows on the user's request.</summary>
public interface IShellService
{
    /// <summary>Opens a URL or folder with its default handler; false if nothing could open it.</summary>
    public bool TryOpen(string target);

    /// <summary>Restarts Windows immediately via System32\shutdown.exe; false if it could not be started.</summary>
    public bool TryRestartWindows();
}
