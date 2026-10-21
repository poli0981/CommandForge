using Microsoft.Win32;

namespace CommandForge.Wpf.Services;

/// <summary><see cref="IFileDialogService"/> backed by the common Windows file dialogs.</summary>
public sealed class FileDialogService : IFileDialogService
{
    /// <inheritdoc />
    public string? PickSaveFile(string title, string filter, string defaultFileName)
    {
        var dialog = new SaveFileDialog { Title = title, Filter = filter, FileName = defaultFileName, AddExtension = true, OverwritePrompt = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? PickOpenFile(string title, string filter)
    {
        var dialog = new OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
