using System.Globalization;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Drives the modal update dialog: check → (up to date | available | error) → download (progress)
/// → apply &amp; restart. The progress callback marshals to the UI thread via <see cref="Progress{T}"/>,
/// mirroring the channel-on-UI-thread pattern used by <see cref="ExecutionViewModel"/>.
/// </summary>
public sealed partial class UpdateViewModel : ObservableObject
{
    private readonly CheckForUpdateUseCase _useCase;
    private CancellationTokenSource? _cancellation;

    public UpdateViewModel(CheckForUpdateUseCase useCase)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        _useCase = useCase;
    }

    /// <summary>Set by the dialog service: returns <see langword="true"/> while a catalog command is still running.</summary>
    public Func<bool>? IsCommandRunning { get; set; }

    /// <summary>Raised when the dialog should close.</summary>
    public event Action? CloseRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChecking))]
    [NotifyPropertyChangedFor(nameof(IsUpToDate))]
    [NotifyPropertyChangedFor(nameof(IsAvailable))]
    [NotifyPropertyChangedFor(nameof(IsDownloading))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    [NotifyCanExecuteChangedFor(nameof(DownloadAndRestartCommand))]
    private UpdateUiState _state = UpdateUiState.Checking;

    [ObservableProperty]
    private string? _newVersion;

    [ObservableProperty]
    private int _percent;

    [ObservableProperty]
    private string _message = string.Empty;

    /// <summary>Whether the latest check completed without an error (an update was found or none exists).</summary>
    public bool CheckSucceeded { get; private set; }

    public bool IsChecking => State == UpdateUiState.Checking;
    public bool IsUpToDate => State == UpdateUiState.UpToDate;
    public bool IsAvailable => State == UpdateUiState.Available;
    public bool IsDownloading => State == UpdateUiState.Downloading;
    public bool IsError => State == UpdateUiState.Error;

    /// <summary>Runs the update check (the manual menu path). The dialog calls this when shown.</summary>
    public async Task CheckAsync()
    {
        State = UpdateUiState.Checking;
        var cancellation = BeginOperation();
        UpdateCheckResult result;
        try
        {
            result = await _useCase.ExecuteAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return; // the dialog was closed while checking
        }
        catch (Exception ex)
        {
            // Never leave the dialog stuck on "Checking": surface a generic failure instead.
            Log.ForContext<UpdateViewModel>().Warning(ex, "Update check failed unexpectedly.");
            Message = ErrorMessage(UpdateError.Unknown);
            State = UpdateUiState.Error;
            return;
        }
        finally
        {
            EndOperation(cancellation);
        }

        CheckSucceeded = result.IsSuccess;
        if (result.HasUpdate)
        {
            SeedAvailable(result.NewVersion);
        }
        else if (result.IsSuccess)
        {
            Message = Strings.Get("Update_UpToDate");
            State = UpdateUiState.UpToDate;
        }
        else
        {
            Message = ErrorMessage(result.Error);
            State = UpdateUiState.Error;
        }
    }

    /// <summary>Pre-seeds the dialog to the "available" state (the startup path already checked).</summary>
    public void SeedAvailable(string? newVersion)
    {
        CheckSucceeded = true;
        NewVersion = newVersion;
        Message = string.Format(CultureInfo.CurrentUICulture, Strings.Get("Update_Available"), newVersion);
        State = UpdateUiState.Available;
    }

    private bool CanDownload() => State == UpdateUiState.Available;

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAndRestartAsync()
    {
        // Applying restarts the process, which would kill a running command (and the elevated
        // session) halfway. Refuse until it finishes — checked again after the download below.
        if (IsCommandRunning?.Invoke() == true)
        {
            Message = Strings.Get("Update_Error_CommandRunning");
            return;
        }

        State = UpdateUiState.Downloading;
        Percent = 0;
        var cancellation = BeginOperation();
        var progress = new Progress<int>(p => Percent = p);

        UpdateError error;
        try
        {
            error = await _useCase.DownloadAsync(progress, cancellation.Token);
            if (error == UpdateError.None)
            {
                if (IsCommandRunning?.Invoke() == true)
                {
                    // A command started while downloading; the update waits (it is already downloaded).
                    Message = Strings.Get("Update_ReadyAfterCommand");
                    State = UpdateUiState.Available;
                    return;
                }

                error = await _useCase.ApplyAndRestartAsync(cancellation.Token);
            }
        }
        catch (OperationCanceledException)
        {
            CloseRequested?.Invoke();
            return;
        }
        finally
        {
            EndOperation(cancellation);
        }

        // Reaching here means apply did not restart (a failure, or a dev/unpackaged build).
        Message = ErrorMessage(error);
        State = UpdateUiState.Error;
    }

    [RelayCommand]
    private void Close()
    {
        _cancellation?.Cancel();
        CloseRequested?.Invoke();
    }

    /// <summary>Cancels a check or download in flight (the window is closing, e.g. via its title-bar X).</summary>
    public void CancelPending() => _cancellation?.Cancel();

    /// <summary>Starts a cancellable operation; <see cref="Close"/> cancels whichever one is in flight.</summary>
    private CancellationTokenSource BeginOperation()
    {
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        return _cancellation;
    }

    private void EndOperation(CancellationTokenSource cancellation)
    {
        if (ReferenceEquals(_cancellation, cancellation))
        {
            _cancellation = null;
        }

        cancellation.Dispose();
    }

    private static string ErrorMessage(UpdateError error) => error switch
    {
        UpdateError.Offline => Strings.Get("Update_Error_Offline"),
        UpdateError.NotFound => Strings.Get("Update_Error_NotFound"),
        UpdateError.RateLimited => Strings.Get("Update_Error_RateLimited"),
        UpdateError.ServerError => Strings.Get("Update_Error_ServerError"),
        UpdateError.NotInstalled => Strings.Get("Update_Error_NotInstalled"),
        _ => Strings.Get("Update_Error_Unknown"),
    };
}
