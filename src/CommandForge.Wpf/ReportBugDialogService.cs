using CommandForge.Application.Ports;
using CommandForge.Wpf.Services;
using CommandForge.Wpf.ViewModels;
using CommandForge.Wpf.Views;

namespace CommandForge.Wpf;

/// <summary><see cref="IReportBugDialogService"/> that shows the modal <see cref="ReportBugDialog"/>.</summary>
public sealed class ReportBugDialogService : IReportBugDialogService
{
    private readonly ILogMaintenance _maintenance;
    private readonly IDialogService _dialogs;
    private readonly IFileDialogService _files;
    private readonly IShellService _shell;

    public ReportBugDialogService(ILogMaintenance maintenance, IDialogService dialogs, IFileDialogService files, IShellService shell)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(shell);
        _maintenance = maintenance;
        _dialogs = dialogs;
        _files = files;
        _shell = shell;
    }

    /// <inheritdoc />
    public void Show(string? errorCode = null, string? errorDetails = null)
    {
        var viewModel = new ReportBugViewModel(_maintenance, _dialogs, _files, _shell, errorCode, errorDetails);
        var dialog = new ReportBugDialog(viewModel);

        // Owner can only be a window that has been shown (this also runs from the crash handler).
        if (System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner)
        {
            dialog.Owner = owner;
        }

        dialog.ShowDialog();
    }
}
