using System.Globalization;
using CommandForge.Application.Ports;
using CommandForge.Application.UseCases;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    public bool IsChecking => State == UpdateUiState.Checking;
    public bool IsUpToDate => State == UpdateUiState.UpToDate;
    public bool IsAvailable => State == UpdateUiState.Available;
    public bool IsDownloading => State == UpdateUiState.Downloading;
    public bool IsError => State == UpdateUiState.Error;

    /// <summary>Runs the update check (the manual menu path). The dialog calls this when shown.</summary>
    public async Task CheckAsync()
    {
        State = UpdateUiState.Checking;
        var result = await _useCase.ExecuteAsync();
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
        NewVersion = newVersion;
        Message = string.Format(CultureInfo.CurrentUICulture, Strings.Get("Update_Available"), newVersion);
        State = UpdateUiState.Available;
    }

    private bool CanDownload() => State == UpdateUiState.Available;

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAndRestartAsync()
    {
        // Phase 3 guard: applying restarts the process, which would orphan a running command and
        // bypass the "block exit while running" prompt. Refuse until the command finishes.
        if (IsCommandRunning?.Invoke() == true)
        {
            Message = Strings.Get("Update_Error_CommandRunning");
            State = UpdateUiState.Error;
            return;
        }

        State = UpdateUiState.Downloading;
        Percent = 0;
        _cancellation = new CancellationTokenSource();
        var progress = new Progress<int>(p => Percent = p);

        UpdateError error;
        try
        {
            error = await _useCase.DownloadAndApplyAsync(progress, _cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            CloseRequested?.Invoke();
            return;
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
