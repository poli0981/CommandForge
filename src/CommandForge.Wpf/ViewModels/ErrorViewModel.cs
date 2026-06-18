using System.Runtime.InteropServices;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>The friendly crash dialog: a generic message + error code, Copy details, Report bug, Close.</summary>
public sealed partial class ErrorViewModel : ObservableObject
{
    private readonly string _details;
    private readonly IReportBugDialogService _reportBug;

    public ErrorViewModel(string errorCode, string details, IReportBugDialogService reportBug)
    {
        ArgumentNullException.ThrowIfNull(reportBug);
        ErrorCode = errorCode;
        _details = details;
        _reportBug = reportBug;
    }

    public string ErrorCode { get; }

    public event Action? CloseRequested;

    [RelayCommand]
    private void CopyDetails()
    {
        try
        {
            Clipboard.SetText(_details);
        }
        catch (COMException)
        {
            // Clipboard temporarily locked — safe to ignore.
        }
    }

    [RelayCommand]
    private void ReportBug()
    {
        _reportBug.Show(ErrorCode, _details);
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
