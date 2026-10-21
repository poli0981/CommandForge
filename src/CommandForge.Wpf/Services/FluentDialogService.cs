using System.Windows;
using System.Windows.Controls;
using CommandForge.Desktop.Resources;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Services;

/// <summary>
/// <see cref="IDialogService"/> that shows Fluent content dialogs in the main window (themed, with
/// "No" as the default answer), falling back to a message box when no in-window dialog can be shown
/// (before the main window exists, or while another dialog is open).
/// </summary>
public sealed class FluentDialogService : IDialogService
{
    private readonly InWindowDialogs _dialogs;
    private readonly MessageBoxDialogService _fallback = new();

    public FluentDialogService(InWindowDialogs dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);
        _dialogs = dialogs;
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, DialogKind kind = DialogKind.Warning)
    {
        if (!_dialogs.CanShow)
        {
            return await _fallback.ConfirmAsync(title, message, kind);
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = Body(message, kind),
            PrimaryButtonText = Strings.Get("Dialog_Yes"),
            PrimaryButtonAppearance = kind == DialogKind.Information ? ControlAppearance.Primary : ControlAppearance.Caution,
            CloseButtonText = Strings.Get("Dialog_No"),
            DefaultButton = ContentDialogButton.Close, // the safe answer is the default
        };

        return await _dialogs.ShowAsync(dialog) == ContentDialogResult.Primary;
    }

    /// <inheritdoc />
    public async Task ShowMessageAsync(string title, string message, DialogKind kind = DialogKind.Information)
    {
        if (!_dialogs.CanShow)
        {
            await _fallback.ShowMessageAsync(title, message, kind);
            return;
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = Body(message, kind),
            CloseButtonText = Strings.Get("Dialog_OK"),
            DefaultButton = ContentDialogButton.Close,
        };

        await _dialogs.ShowAsync(dialog);
    }

    /// <summary>The message with an icon that says the same as its color (never color alone).</summary>
    private static UIElement Body(string message, DialogKind kind)
    {
        var (symbol, brushKey) = kind switch
        {
            DialogKind.Error => (SymbolRegular.ErrorCircle24, "SystemFillColorCriticalBrush"),
            DialogKind.Warning => (SymbolRegular.Warning24, "SystemFillColorCautionBrush"),
            _ => (SymbolRegular.Info24, "AccentTextFillColorPrimaryBrush"),
        };

        var icon = new SymbolIcon(symbol, 24, filled: false)
        {
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 12, 0),
        };
        icon.SetResourceReference(IconElement.ForegroundProperty, brushKey);

        var text = new System.Windows.Controls.TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 460 };

        var panel = new DockPanel();
        DockPanel.SetDock(icon, Dock.Left);
        panel.Children.Add(icon);
        panel.Children.Add(text);
        return panel;
    }
}
