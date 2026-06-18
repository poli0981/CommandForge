using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CommandForge.Application.Logging;
using CommandForge.Application.Ports;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// The Log Viewer: live in-memory log entries with level/text filtering, color coding, and export.
/// Marshals the off-thread <see cref="ILogReader.EntriesChanged"/> onto the UI thread.
/// </summary>
public sealed partial class LogViewerViewModel : ObservableObject
{
    private readonly ILogReader _logReader;
    private readonly ILogMaintenance _maintenance;

    public LogViewerViewModel(ILogReader logReader, ILogMaintenance maintenance)
    {
        ArgumentNullException.ThrowIfNull(logReader);
        ArgumentNullException.ThrowIfNull(maintenance);
        _logReader = logReader;
        _maintenance = maintenance;

        LevelOptions =
        [
            new LogFilterOption(LogLevelFilter.All, Strings.Get("LogLevel_All")),
            new LogFilterOption(LogLevelFilter.Information, Strings.Get("LogLevel_Info")),
            new LogFilterOption(LogLevelFilter.Warning, Strings.Get("LogLevel_Warning")),
            new LogFilterOption(LogLevelFilter.Error, Strings.Get("LogLevel_Error")),
            new LogFilterOption(LogLevelFilter.Debug, Strings.Get("LogLevel_Debug")),
        ];

        View = CollectionViewSource.GetDefaultView(Entries);
        View.Filter = o => o is LogEntryViewModel entry && Matches(entry, SelectedLevel, FilterText);

        Reload();
        _logReader.EntriesChanged += OnEntriesChanged;
    }

    public ObservableCollection<LogEntryViewModel> Entries { get; } = [];

    public ICollectionView View { get; }

    public IReadOnlyList<LogFilterOption> LevelOptions { get; }

    [ObservableProperty]
    private LogLevelFilter _selectedLevel = LogLevelFilter.All;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    partial void OnSelectedLevelChanged(LogLevelFilter value) => View.Refresh();

    partial void OnFilterTextChanged(string value) => View.Refresh();

    [RelayCommand]
    private void OpenFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo(_maintenance.LogsDirectoryPath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // Folder missing / shell failure — ignore.
        }
    }

    [RelayCommand]
    private async Task ExportZipAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = Strings.Get("LogViewer_Export"),
            Filter = "Zip archive (*.zip)|*.zip",
            FileName = $"commandforge-logs-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _maintenance.ExportZipAsync(dialog.FileName);
        MessageBox.Show(
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Log_ExportSuccess"), dialog.FileName),
            Strings.Get("LogViewer_Title"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void Clear()
    {
        _logReader.Clear();
        Entries.Clear();
    }

    /// <summary>Pure filter predicate (level bucket + case-insensitive text), extracted for tests.</summary>
    public static bool Matches(LogEntryViewModel entry, LogLevelFilter level, string? text)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var levelOk = level switch
        {
            LogLevelFilter.All => true,
            LogLevelFilter.Information => entry.Level == LogLevel.Information,
            LogLevelFilter.Warning => entry.Level == LogLevel.Warning,
            LogLevelFilter.Error => entry.Level is LogLevel.Error or LogLevel.Fatal,
            LogLevelFilter.Debug => entry.Level is LogLevel.Debug or LogLevel.Verbose,
            _ => true,
        };

        return levelOk && (string.IsNullOrEmpty(text) || entry.Message.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private void OnEntriesChanged(object? sender, EventArgs e)
        => System.Windows.Application.Current?.Dispatcher.InvokeAsync(Reload);

    private void Reload()
    {
        Entries.Clear();
        foreach (var entry in _logReader.GetRecentEntries())
        {
            Entries.Add(new LogEntryViewModel(entry));
        }
    }
}
