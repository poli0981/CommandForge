using CommandForge.Application.Ports;
using CommandForge.Wpf.ViewModels;
using CommandForge.Wpf.Views;

namespace CommandForge.Wpf;

/// <summary><see cref="IReportBugDialogService"/> that shows the modal <see cref="ReportBugDialog"/>.</summary>
public sealed class ReportBugDialogService : IReportBugDialogService
{
    private readonly ILogMaintenance _maintenance;

    public ReportBugDialogService(ILogMaintenance maintenance)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        _maintenance = maintenance;
    }

    /// <inheritdoc />
    public void Show(string? errorCode = null, string? errorDetails = null)
    {
        var viewModel = new ReportBugViewModel(_maintenance, errorCode, errorDetails);
        var dialog = new ReportBugDialog(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        dialog.ShowDialog();
    }
}
