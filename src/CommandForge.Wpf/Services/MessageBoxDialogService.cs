using System.Windows;

namespace CommandForge.Desktop.Services;

/// <summary><see cref="IDialogService"/> backed by the Windows message box.</summary>
public sealed class MessageBoxDialogService : IDialogService
{
    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, DialogKind kind = DialogKind.Warning)
    {
        var result = Show(message, title, MessageBoxButton.YesNo, Icon(kind), MessageBoxResult.No);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    /// <inheritdoc />
    public Task ShowMessageAsync(string title, string message, DialogKind kind = DialogKind.Information)
    {
        Show(message, title, MessageBoxButton.OK, Icon(kind), MessageBoxResult.OK);
        return Task.CompletedTask;
    }

    private static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult)
        => System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner
            ? MessageBox.Show(owner, message, title, buttons, icon, defaultResult)
            : MessageBox.Show(message, title, buttons, icon, defaultResult);

    private static MessageBoxImage Icon(DialogKind kind) => kind switch
    {
        DialogKind.Error => MessageBoxImage.Error,
        DialogKind.Warning => MessageBoxImage.Warning,
        _ => MessageBoxImage.Information,
    };
}
