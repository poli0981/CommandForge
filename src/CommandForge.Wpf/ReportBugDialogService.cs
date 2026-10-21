using CommandForge.Application.Ports;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;
using CommandForge.Desktop.Views;

namespace CommandForge.Desktop;

/// <summary><see cref="IReportBugDialogService"/> that shows the in-window <see cref="ReportBugDialog"/>.</summary>
public sealed class ReportBugDialogService : IReportBugDialogService
{
    private readonly ILogMaintenance _maintenance;
    private readonly IDialogService _dialogService;
    private readonly IFileDialogService _files;
    private readonly IShellService _shell;
    private readonly InWindowDialogs _dialogs;

    public ReportBugDialogService(ILogMaintenance maintenance, IDialogService dialogService, IFileDialogService files, IShellService shell, InWindowDialogs dialogs)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(dialogs);
        _maintenance = maintenance;
        _dialogService = dialogService;
        _files = files;
        _shell = shell;
        _dialogs = dialogs;
    }

    /// <inheritdoc />
    public async Task ShowAsync(string? errorCode = null, string? errorDetails = null)
    {
        var viewModel = new ReportBugViewModel(_maintenance, _dialogService, _files, _shell, errorCode, errorDetails);

        // A crash before the main window appeared (or with another dialog open): go straight to the
        // prefilled issue form, which carries the same environment details.
        if (!_dialogs.CanShow)
        {
            viewModel.OpenIssueCommand.Execute(null);
            return;
        }

        await _dialogs.ShowAsync(new ReportBugDialog(viewModel));
    }
}
