namespace CommandForge.Wpf.Services;

/// <summary>Clipboard access for view-models.</summary>
public interface IClipboardService
{
    /// <summary>Copies <paramref name="text"/>; false when another process holds the clipboard.</summary>
    public bool TrySetText(string text);
}
