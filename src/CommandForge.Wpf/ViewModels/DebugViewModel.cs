using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using CommandForge.Application.Ports;
using CommandForge.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>Developer Debug panel: verbose log dump, catalog stats + validation errors, and a system-info dump.</summary>
public sealed partial class DebugViewModel : ObservableObject
{
    private readonly ILogReader _logReader;

    public DebugViewModel(ILogReader logReader, ICatalogProvider catalog)
    {
        ArgumentNullException.ThrowIfNull(logReader);
        ArgumentNullException.ThrowIfNull(catalog);
        _logReader = logReader;

        CategoryCount = catalog.GetCategories().Count;
        CommandCount = catalog.GetCommands().Count;
        ValidationErrors = new ObservableCollection<string>(catalog.ValidationErrors);
        SystemInfo = BuildSystemInfo();

        Refresh();
        _logReader.EntriesChanged += (_, _) => System.Windows.Application.Current?.Dispatcher.InvokeAsync(Refresh);
    }

    public int CategoryCount { get; }

    public int CommandCount { get; }

    public ObservableCollection<string> ValidationErrors { get; }

    public bool HasValidationErrors => ValidationErrors.Count > 0;

    public string SystemInfo { get; }

    [ObservableProperty]
    private string _logText = string.Empty;

    [RelayCommand]
    private void Refresh()
    {
        var builder = new StringBuilder();
        foreach (var entry in _logReader.GetRecentEntries())
        {
            builder.Append(entry.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture))
                .Append(" [").Append(entry.Level).Append("] ").AppendLine(entry.Message);
        }

        LogText = builder.ToString();
    }

    [RelayCommand]
    private void CopySystemInfo()
    {
        try
        {
            Clipboard.SetText(SystemInfo);
        }
        catch (COMException)
        {
            // Clipboard temporarily locked — safe to ignore.
        }
    }

    /// <summary>Builds the read-only system-info dump (pure; testable).</summary>
    public static string BuildSystemInfo()
    {
        var version = typeof(DebugViewModel).Assembly.GetName().Version;
        var builder = new StringBuilder();
        builder.AppendLine($"App version: {(version is null ? "—" : $"{version.Major}.{version.Minor}.{version.Build}")}");
        builder.AppendLine($"OS: {RuntimeInformation.OSDescription}");
        builder.AppendLine($"OS architecture: {RuntimeInformation.OSArchitecture}");
        builder.AppendLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
        builder.AppendLine($".NET: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"Data: {AppPaths.DataDirectory}");
        builder.AppendLine($"Logs: {AppPaths.LogsDirectory}");
        builder.AppendLine($"Config: {AppPaths.ConfigFilePath}");
        return builder.ToString();
    }
}
