using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Windows;
using CommandForge.Application.UseCases;
using CommandForge.Domain;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Hosts a single command run: live console output (streamed off the UI thread via a channel),
/// cancellation, and the result (exit code, duration, restart-required).
/// </summary>
public sealed partial class ExecutionViewModel : ObservableObject
{
    private readonly RunCommandUseCase _run;
    private CancellationTokenSource? _cancellation;

    public ExecutionViewModel(RunCommandUseCase run) => _run = run;

    /// <summary>Live output lines (stdout + stderr).</summary>
    public ObservableCollection<OutputLine> OutputLines { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowConsole))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowConsole))]
    private bool _hasResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _success;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _cancelled;

    [ObservableProperty]
    private bool _requiresRestart;

    [ObservableProperty]
    private int _exitCode;

    [ObservableProperty]
    private string _durationText = string.Empty;

    /// <summary>Whether the console panel should be visible (running or has a result).</summary>
    public bool ShowConsole => IsRunning || HasResult;

    /// <summary>Localized result status (Success / Failed / Cancelled).</summary>
    public string StatusText => Cancelled
        ? Strings.Get("Result_Cancelled")
        : Success ? Strings.Get("Result_Success") : Strings.Get("Result_Failed");

    /// <summary>
    /// Runs <paramref name="command"/>, streaming output to <see cref="OutputLines"/>. If
    /// <paramref name="restorePointCommand"/> is supplied it runs first (before a risky command);
    /// both go through the same broker, so they share one UAC prompt.
    /// </summary>
    public async Task RunAsync(CommandDefinition command, CommandDefinition? restorePointCommand = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (IsRunning)
        {
            return;
        }

        OutputLines.Clear();
        HasResult = false;
        IsRunning = true;
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;

        try
        {
            if (restorePointCommand is not null)
            {
                OutputLines.Add(new OutputLine(Strings.Get("Restore_Creating"), IsError: false));
                var restoreResult = await RunOneAsync(restorePointCommand, token);
                if (restoreResult.Cancelled)
                {
                    ApplyResult(restoreResult);
                    return;
                }

                OutputLines.Add(new OutputLine(
                    Strings.Get(restoreResult.Success ? "Restore_Created" : "Restore_Failed"),
                    IsError: !restoreResult.Success));
            }

            ApplyResult(await RunOneAsync(command, token));
        }
        finally
        {
            IsRunning = false;
            _cancellation.Dispose();
            _cancellation = null;
        }
    }

    private async Task<ExecutionResult> RunOneAsync(CommandDefinition command, CancellationToken token)
    {
        var channel = Channel.CreateBounded<OutputLine>(
            new BoundedChannelOptions(4096) { SingleReader = true, SingleWriter = false });

        // Executor runs off the UI thread; we consume on the UI thread (safe to touch the collection).
        var execution = Task.Run(() => _run.RunAsync(command, channel.Writer, token), CancellationToken.None);

        await foreach (var line in channel.Reader.ReadAllAsync(CancellationToken.None))
        {
            OutputLines.Add(line);
        }

        return await execution;
    }

    private void ApplyResult(ExecutionResult result)
    {
        ExitCode = result.ExitCode;
        Success = result.Success;
        Cancelled = result.Cancelled;
        RequiresRestart = result.RequiresRestart;
        DurationText = FormatDuration(result.Duration);
        HasResult = true;
    }

    [RelayCommand]
    private void Cancel() => _cancellation?.Cancel();

    [RelayCommand]
    private void CopyOutput()
    {
        var text = string.Join(Environment.NewLine, OutputLines.Select(l => l.Text));
        try
        {
            Clipboard.SetText(text);
        }
        catch (COMException)
        {
            // Clipboard temporarily locked — safe to ignore.
        }
    }

    private static string FormatDuration(TimeSpan duration)
        => duration.TotalSeconds < 1
            ? $"{duration.TotalMilliseconds:F0} ms"
            : $"{duration.TotalSeconds:F1} s";
}
