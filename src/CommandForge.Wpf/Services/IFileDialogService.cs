namespace CommandForge.Wpf.Services;

/// <summary>Open/save file pickers for view-models.</summary>
public interface IFileDialogService
{
    /// <summary>Asks where to save; returns the chosen path or null if cancelled.</summary>
    public string? PickSaveFile(string title, string filter, string defaultFileName);

    /// <summary>Asks for a file to open; returns the chosen path or null if cancelled.</summary>
    public string? PickOpenFile(string title, string filter);
}
