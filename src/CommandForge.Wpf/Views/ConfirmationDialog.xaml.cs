using System.Windows.Input;
using CommandForge.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>In-window confirmation for Caution/Dangerous commands (golden rule #8).</summary>
public partial class ConfirmationDialog : ContentDialog
{
    private readonly ConfirmationViewModel _viewModel;

    public ConfirmationDialog(ConfirmationViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();

        // Implicit styles match the exact type only: take ContentDialog's template explicitly.
        SetResourceReference(StyleProperty, typeof(ContentDialog));
        _viewModel = viewModel;
        DataContext = viewModel;
        PrimaryButtonAppearance = viewModel.IsDangerous ? ControlAppearance.Danger : ControlAppearance.Primary;

        // For Dangerous commands start in the type-to-confirm box; otherwise focus stays on Cancel.
        if (viewModel.IsDangerous)
        {
            Opened += (_, _) => Keyboard.Focus(ConfirmationInputBox);
        }
    }

    protected override void OnButtonClick(ContentDialogButton button)
    {
        // Run only counts once the name is typed (the button is disabled until then, but a key
        // press can still reach here).
        if (button == ContentDialogButton.Primary && !_viewModel.CanRun)
        {
            return;
        }

        base.OnButtonClick(button);
    }
}
