using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Wpf.Resources;
using CommandForge.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Drives the Report-a-bug dialog: shows auto-gathered environment, exports logs, and opens a
/// prefilled GitHub issue. User-initiated only — nothing is sent automatically (Privacy).
/// </summary>
public sealed partial class ReportBugViewModel : ObservableObject
{
    // Browsers and GitHub cap URL length; the body beyond this is left for the user to paste.
    private const int MaxBodyCharsInUrl = 4000;

    private readonly ILogMaintenance _maintenance;
    private readonly IDialogService _dialogs;
    private readonly IFileDialogService _files;
    private readonly IShellService _shell;
    private readonly string? _errorCode;

    public ReportBugViewModel(
        ILogMaintenance maintenance,
        IDialogService dialogs,
        IFileDialogService files,
        IShellService shell,
        string? errorCode = null,
        string? errorDetails = null)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(shell);
        _maintenance = maintenance;
        _dialogs = dialogs;
        _files = files;
        _shell = shell;
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
        var title = Strings.Get("ReportBug_ExportLog");
        var path = _files.PickSaveFile(title, Strings.Get("FileFilter_Zip"), $"commandforge-logs-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
        if (path is null)
        {
            return;
        }

        try
        {
            await _maintenance.ExportZipAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await _dialogs.ShowMessageAsync(title, string.Format(CultureInfo.CurrentCulture, Strings.Get("File_SaveFailed"), ex.Message), DialogKind.Error);
            return;
        }

        await _dialogs.ShowMessageAsync(
            Strings.Get("ReportBug_Title"),
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Log_ExportSuccess"), path)
                + Environment.NewLine + Strings.Get("ReportBug_ReviewReminder"));
    }

    [RelayCommand]
    private void OpenIssue()
    {
        var body = TruncateForUrl(BuildIssueBody(EnvironmentText, Description, _errorCode));
        _shell.TryOpen($"{ProductLinks.NewIssue}?title={Uri.EscapeDataString(BuildTitle())}&body={Uri.EscapeDataString(body)}");
    }

    /// <summary>Pure: keeps a prefilled issue body within URL limits (a full stack trace can exceed them).</summary>
    public static string TruncateForUrl(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return body.Length <= MaxBodyCharsInUrl
            ? body
            : body[..MaxBodyCharsInUrl] + Environment.NewLine + Environment.NewLine + "_(truncated — please paste the rest of the details here)_";
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
