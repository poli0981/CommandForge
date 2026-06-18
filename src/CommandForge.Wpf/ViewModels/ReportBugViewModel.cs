using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using CommandForge.Application.Ports;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Drives the Report-a-bug dialog: shows auto-gathered environment, exports logs, and opens a
/// prefilled GitHub issue. User-initiated only — nothing is sent automatically (Privacy).
/// </summary>
public sealed partial class ReportBugViewModel : ObservableObject
{
    private const string IssueBaseUrl = "https://github.com/poli0981/CommandForge/issues/new";

    private readonly ILogMaintenance _maintenance;
    private readonly string? _errorCode;

    public ReportBugViewModel(ILogMaintenance maintenance, string? errorCode = null, string? errorDetails = null)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        _maintenance = maintenance;
        _errorCode = errorCode;
        EnvironmentText = BuildEnvironment(AppVersion(), RuntimeInformation.OSDescription, RuntimeInformation.OSArchitecture);
        _description = errorDetails ?? string.Empty;
    }

    public string EnvironmentText { get; }

    [ObservableProperty]
    private string _description;

    public event Action? CloseRequested;

    [RelayCommand]
    private async Task ExportLogAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = Strings.Get("ReportBug_ExportLog"),
            Filter = "Zip archive (*.zip)|*.zip",
            FileName = $"commandforge-logs-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _maintenance.ExportZipAsync(dialog.FileName);
        MessageBox.Show(
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Log_ExportSuccess"), dialog.FileName)
                + Environment.NewLine + Strings.Get("ReportBug_ReviewReminder"),
            Strings.Get("ReportBug_Title"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OpenIssue()
    {
        var body = BuildIssueBody(EnvironmentText, Description, _errorCode);
        var url = $"{IssueBaseUrl}?title={Uri.EscapeDataString(BuildTitle())}&body={Uri.EscapeDataString(body)}";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // Browser launch failed — ignore.
        }
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    /// <summary>Pure: assembles the environment block (testable).</summary>
    public static string BuildEnvironment(string appVersion, string osDescription, Architecture osArchitecture)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"App version: {appVersion}");
        builder.AppendLine($"OS: {osDescription}");
        builder.AppendLine($"Architecture: {osArchitecture}");
        return builder.ToString();
    }

    /// <summary>Pure: assembles the GitHub issue body template (testable).</summary>
    public static string BuildIssueBody(string environment, string? userDescription, string? errorCode = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Description");
        builder.AppendLine(string.IsNullOrWhiteSpace(userDescription) ? "_Describe what happened._" : userDescription);
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            builder.AppendLine($"Error code: {errorCode}");
            builder.AppendLine();
        }

        builder.AppendLine("## Environment");
        builder.AppendLine("```");
        builder.Append(environment);
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Logs");
        builder.AppendLine("_Attach the exported .zip here. Review its contents first — it may contain command output._");
        return builder.ToString();
    }

    private string BuildTitle() => _errorCode is null ? "[Bug] " : $"[Crash {_errorCode}] ";

    private static string AppVersion()
    {
        var version = typeof(ReportBugViewModel).Assembly.GetName().Version;
        return version is null ? "—" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
