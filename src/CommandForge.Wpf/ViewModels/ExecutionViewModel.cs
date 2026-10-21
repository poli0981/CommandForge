using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Channels;
using CommandForge.Application;
using CommandForge.Application.Ports;
using CommandForge.Application.Recipes;
using CommandForge.Application.UseCases;
using CommandForge.Desktop.Resources;
using CommandForge.Desktop.Services;
using CommandForge.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace CommandForge.Desktop.ViewModels;

/// <summary>
/// Hosts a single command run: live console output (streamed off the UI thread via a channel and
/// appended in batches), cancellation, the optional restore point before it, and the result (exit
/// code, duration, restart-required).
/// </summary>
public sealed partial class ExecutionViewModel : ObservableObject
{
    // Lines moved to the UI per dispatcher turn, and the pause after a full batch so WPF can render.
    private const int MaxBatch = 500;
    private static readonly TimeSpan BatchPause = TimeSpan.FromMilliseconds(15);

    private readonly RunCommandUseCase _run;
    private readonly IElevationService _elevation;
    private readonly IDialogService _dialogs;
    private readonly IClipboardService _clipboard;
    private readonly IFileDialogService _files;
    private readonly IShellService _shell;
    private readonly INotificationService _notifications;
    private readonly ILogger _log = Log.ForContext<ExecutionViewModel>();
    private CancellationTokenSource? _cancellation;

    public ExecutionViewModel(
        RunCommandUseCase run,
        IElevationService elevation,
        IDialogService dialogs,
        IClipboardService clipboard,
        IFileDialogService files,
        IShellService shell,
        INotificationService notifications)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(elevation);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(notifications);
        _run = run;
        _elevation = elevation;
        _dialogs = dialogs;
        _clipboard = clipboard;
        _files = files;
        _shell = shell;
        _notifications = notifications;

        OutputLines.PropertyChanged += OnOutputLinesChanged;

        // The UAC prompt was answered: the "waiting for permission" status no longer applies.
        _elevation.SessionChanged += (_, _) => System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (_elevation.IsSessionActive)
            {
                IsWaitingForElevation = false;
            }
        });
    }

    /// <summary>Live output lines (stdout + stderr), capped at <see cref="OutputBuffer.Capacity"/>.</summary>
    public OutputBuffer OutputLines { get; } = new();

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

    /// <summary>Whether the console follows new output (the user's setting; scrolling up pauses it).</summary>
    [ObservableProperty]
    private bool _autoScroll = true;

    /// <summary>Whether an admin command is waiting for the user to answer the UAC prompt.</summary>
    [ObservableProperty]
    private bool _isWaitingForElevation;

    /// <summary>The title of the command or recipe that ran last (for notifications).</summary>
    [ObservableProperty]
    private string _lastRunTitle = string.Empty;

    /// <summary>Whether older output lines were dropped to stay within the console's capacity.</summary>
    public bool HasDroppedLines => OutputLines.DroppedCount > 0;

    /// <summary>How many older output lines are not shown.</summary>
    public string DroppedLinesText
        => string.Format(CultureInfo.CurrentCulture, Strings.Get("Console_DroppedLines"), OutputLines.DroppedCount);

    /// <summary>Whether the console panel should be visible (running or has a result).</summary>
    public bool ShowConsole => IsRunning || HasResult;

    /// <summary>
    /// The result of the most recent finished run (the main command, not the restore point),
    /// or null if nothing has run yet. Used by callers to record execution history.
    /// </summary>
    public ExecutionResult? LastResult { get; private set; }

    /// <summary>Localized result status (Success / Failed / Cancelled).</summary>
    public string StatusText => (Cancelled, Success) switch
    {
        (true, _) => Strings.Get("Result_Cancelled"),
        (false, true) => Strings.Get("Result_Success"),
        _ => Strings.Get("Result_Failed"),
    };

    /// <summary>
    /// Runs <paramref name="command"/>, streaming output to <see cref="OutputLines"/>. If
    /// <paramref name="restorePointCommand"/> is supplied it runs first (before a risky command);
    /// both go through the same broker, so they share one UAC prompt. Returns false when the command
    /// did not run (another run is in progress, or the user stopped after a failed restore point).
    /// </summary>
    public async Task<bool> RunAsync(CommandDefinition command, CommandDefinition? restorePointCommand = null, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (IsRunning)
        {
            return false;
        }

        LastRunTitle = title ?? Strings.Get(command.TitleKey);
        var token = Begin();
        try
        {
            if (restorePointCommand is not null
                && !await PrepareRestorePointAsync(restorePointCommand, Strings.Get(command.TitleKey), token))
            {
                return false;
            }

            var result = await RunOneAsync(command, token);
            ApplyResult(result);
            LogSummary(command, result);
            return true;
        }
        finally
        {
            End();
        }
    }

    /// <summary>
    /// Runs a recipe's <paramref name="steps"/> sequentially through the same broker session (one
    /// UAC prompt for an all-admin chain), streaming output with a header per step. Stops on the
    /// first failure, cancellation, or reboot-required step (see <see cref="RecipeFlow.ShouldStop"/>).
    /// Returns the (commandId, result) of each executed step, for history recording.
    /// </summary>
    public async Task<IReadOnlyList<(string CommandId, ExecutionResult Result)>> RunRecipeAsync(
        IReadOnlyList<CommandDefinition> steps,
        string recipeName,
        CommandDefinition? restorePointCommand = null)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var executed = new List<(string CommandId, ExecutionResult Result)>();
        if (IsRunning || steps.Count == 0)
        {
            return executed;
        }

        LastRunTitle = recipeName;
        var token = Begin();
        ExecutionResult? last = null;
        try
        {
            if (restorePointCommand is not null && !await PrepareRestorePointAsync(restorePointCommand, recipeName, token))
            {
                return executed;
            }

            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                OutputLines.Add(new OutputLine(
                    string.Format(CultureInfo.CurrentCulture, Strings.Get("Recipe_StepHeader"), i + 1, steps.Count, Strings.Get(step.TitleKey)),
                    IsError: false));

                var result = await RunOneAsync(step, token);
                last = result;
                executed.Add((step.Id, result));
                LogSummary(step, result);

                if (RecipeFlow.ShouldStop(result))
                {
                    OutputLines.Add(new OutputLine(Strings.Get(StopReasonKey(result)), IsError: !result.Success && !result.Cancelled));
                    break;
                }
            }

            if (last is not null)
            {
                ApplyResult(last);
            }
        }
        finally
        {
            End();
        }

        return executed;
    }

    /// <summary>
    /// Creates the restore point. "Already made one in the last 24 hours" counts as fine; any other
    /// failure asks whether to continue without one (No is the default). Returns false to stop.
    /// </summary>
    private async Task<bool> PrepareRestorePointAsync(CommandDefinition restorePointCommand, string targetTitle, CancellationToken token)
    {
        OutputLines.Add(new OutputLine(Strings.Get("Restore_Creating"), IsError: false));
        var result = await RunOneAsync(restorePointCommand, token);

        if (result.Cancelled)
        {
            ApplyResult(result);
            return false;
        }

        if (result.Success)
        {
            OutputLines.Add(new OutputLine(Strings.Get("Restore_Created"), IsError: false));
            return true;
        }

        if (result.ExitCode == RunSafety.RestorePointSkippedExitCode)
        {
            OutputLines.Add(new OutputLine(Strings.Get("Restore_Skipped"), IsError: false));
            return true;
        }

        OutputLines.Add(new OutputLine(Strings.Get("Restore_Failed"), IsError: true));
        var proceed = await _dialogs.ConfirmAsync(
            Strings.Get("Restore_FailedTitle"),
            string.Format(CultureInfo.CurrentCulture, Strings.Get("Restore_FailedPrompt"), targetTitle));
        if (!proceed)
        {
            OutputLines.Add(new OutputLine(Strings.Get("Run_NotStarted"), IsError: true));
            ApplyResult(result);
        }

        return proceed;
    }

    private async Task<ExecutionResult> RunOneAsync(CommandDefinition command, CancellationToken token)
    {
        var channel = Channel.CreateBounded<OutputLine>(
            new BoundedChannelOptions(4096) { SingleReader = true, SingleWriter = false });

        // Until the UAC prompt is answered an admin command shows "waiting for permission".
        IsWaitingForElevation = command.RequiresAdmin && !_elevation.IsSessionActive;

        // Executor runs off the UI thread; we consume on the UI thread (safe to touch the buffer).
        var execution = Task.Run(() => _run.RunAsync(command, channel.Writer, token), CancellationToken.None);

        var batch = new List<OutputLine>(MaxBatch);
        while (await channel.Reader.WaitToReadAsync(CancellationToken.None))
        {
            while (batch.Count < MaxBatch && channel.Reader.TryRead(out var line))
            {
                batch.Add(line);
            }

            IsWaitingForElevation = false;
            OutputLines.Append(batch);
            var full = batch.Count == MaxBatch;
            batch.Clear();
            if (full)
            {
                await Task.Delay(BatchPause, CancellationToken.None); // a fast producer: let WPF render
            }
        }

        IsWaitingForElevation = false;
        try
        {
            return await execution;
        }
        catch (Exception ex)
        {
            // RunCommandUseCase already maps cancellation; anything else is unexpected — report, don't crash.
            _log.Error(ex, "Running {CommandId} failed unexpectedly.", command.Id);
            OutputLines.Add(new OutputLine(string.Format(CultureInfo.CurrentCulture, Strings.Get("Run_UnexpectedError"), ex.Message), IsError: true));
            return new ExecutionResult { ExitCode = -1, Success = false, Duration = TimeSpan.Zero };
        }
    }

    private CancellationToken Begin()
    {
        OutputLines.Clear();
        HasResult = false;
        IsRunning = true;
        _cancellation = new CancellationTokenSource();
        return _cancellation.Token;
    }

    private void End()
    {
        IsRunning = false;
        _cancellation?.Dispose();
        _cancellation = null;
    }

    private void ApplyResult(ExecutionResult result)
    {
        LastResult = result;
        ExitCode = result.ExitCode;
        Success = result.Success;
        Cancelled = result.Cancelled;
        RequiresRestart = result.RequiresRestart;
        DurationText = FormatDuration(result.Duration);
        HasResult = true;
    }

    private void LogSummary(CommandDefinition command, ExecutionResult result)
        => _log.Information(
            "Ran {CommandId} (admin: {RequiresAdmin}): exit {ExitCode}, success {Success}, cancelled {Cancelled}, restart {RequiresRestart}, {DurationMs} ms.",
            command.Id,
            command.RequiresAdmin,
            result.ExitCode,
            result.Success,
            result.Cancelled,
            result.RequiresRestart,
            (long)result.Duration.TotalMilliseconds);

    private static string StopReasonKey(ExecutionResult result)
    {
        if (result.Cancelled)
        {
            return "Recipe_Cancelled";
        }

        return result.RequiresRestart ? "Recipe_StoppedRestart" : "Recipe_StoppedOnError";
    }

    [RelayCommand]
    private void Cancel() => _cancellation?.Cancel();

    [RelayCommand]
    private void CopyOutput()
    {
        if (_clipboard.TrySetText(OutputLines.ToText()))
        {
            _notifications.Show(Strings.Get("Snack_Copied"), NotificationKind.Success);
        }
        else
        {
            _notifications.Show(Strings.Get("Snack_CopyFailed"), NotificationKind.Warning);
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        var title = Strings.Get("Console_Save");
        var path = _files.PickSaveFile(title, Strings.Get("FileFilter_Text"), $"commandforge-output-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        if (path is null)
        {
            return;
        }

        try
        {
            await File.WriteAllTextAsync(path, OutputLines.ToText() + Environment.NewLine, CancellationToken.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await _dialogs.ShowMessageAsync(title, string.Format(CultureInfo.CurrentCulture, Strings.Get("File_SaveFailed"), ex.Message), DialogKind.Error);
            return;
        }

        _notifications.Show(string.Format(CultureInfo.CurrentCulture, Strings.Get("Console_Saved"), path), NotificationKind.Success);
    }

    /// <summary>Restarts Windows after a command that needs it, only on request and never while a command runs.</summary>
    [RelayCommand]
    private async Task RestartWindowsAsync()
    {
        if (IsRunning)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync(Strings.Get("Restart_ConfirmTitle"), Strings.Get("Restart_ConfirmMessage")))
        {
            return;
        }

        if (!_shell.TryRestartWindows())
        {
            await _dialogs.ShowMessageAsync(Strings.Get("Restart_ConfirmTitle"), Strings.Get("Restart_Failed"), DialogKind.Error);
        }
    }

    private void OnOutputLinesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OutputBuffer.DroppedCount))
        {
            OnPropertyChanged(nameof(HasDroppedLines));
            OnPropertyChanged(nameof(DroppedLinesText));
        }
    }

    private static string FormatDuration(TimeSpan duration)
        => duration.TotalSeconds < 1
            ? string.Format(CultureInfo.CurrentCulture, Strings.Get("Duration_Milliseconds"), duration.TotalMilliseconds)
            : string.Format(CultureInfo.CurrentCulture, Strings.Get("Duration_Seconds"), duration.TotalSeconds);
}
