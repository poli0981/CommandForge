using CommandForge.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Desktop.ViewModels;

/// <summary>The friendly crash dialog: a generic message + error code, Copy details, Report bug, Close.</summary>
public sealed partial class ErrorViewModel : ObservableObject
{
    private readonly string _details;
    private readonly IReportBugDialogService _reportBug;
    private readonly IClipboardService _clipboard;

    public ErrorViewModel(string errorCode, string details, IReportBugDialogService reportBug, IClipboardService clipboard)
    {
        ArgumentNullException.ThrowIfNull(reportBug);
        ArgumentNullException.ThrowIfNull(clipboard);
        ErrorCode = errorCode;
        _details = details;
        _reportBug = reportBug;
        _clipboard = clipboard;
    }

    public string ErrorCode { get; }

    public event Action? CloseRequested;

    [RelayCommand]
    private void CopyDetails() => _clipboard.TrySetText(_details);

    [RelayCommand]
    private void ReportBug()
    {
        // Close first: the report opens in the main window, which this modal dialog blocks.
        CloseRequested?.Invoke();
        _ = _reportBug.ShowAsync(ErrorCode, _details);
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
