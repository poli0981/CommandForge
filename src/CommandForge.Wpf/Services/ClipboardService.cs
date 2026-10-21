using System.Runtime.InteropServices;
using System.Windows;

namespace CommandForge.Wpf.Services;

/// <summary><see cref="IClipboardService"/> backed by the WPF clipboard.</summary>
public sealed class ClipboardService : IClipboardService
{
    /// <inheritdoc />
    public bool TrySetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (COMException)
        {
            return false; // the clipboard is momentarily locked by another process
        }
    }
}
