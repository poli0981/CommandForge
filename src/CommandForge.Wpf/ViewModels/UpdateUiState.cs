namespace CommandForge.Wpf.ViewModels;

/// <summary>The state of the modal update flow.</summary>
public enum UpdateUiState
{
    /// <summary>Contacting GitHub Releases.</summary>
    Checking,

    /// <summary>Already on the latest version.</summary>
    UpToDate,

    /// <summary>A newer version is available to download.</summary>
    Available,

    /// <summary>Downloading the update (progress 0-100).</summary>
    Downloading,

    /// <summary>The check or download failed.</summary>
    Error,
}
