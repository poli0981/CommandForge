using System.ComponentModel;
using System.Windows.Controls;
using CommandForge.Desktop.Services;
using CommandForge.Desktop.ViewModels;

namespace CommandForge.Desktop.Views;

/// <summary>The recipes view (build/run command chains).</summary>
public partial class RecipesView : UserControl
{
    private RecipesViewModel? _viewModel;

    public RecipesView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Attach(DataContext as RecipesViewModel);
    }

    private void Attach(RecipesViewModel? viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    // The warning bar above the steps appears silently otherwise.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RecipesViewModel.UnavailableStepsNotice) && IsVisible)
        {
            ScreenReader.Announce(_viewModel?.UnavailableStepsNotice);
        }
    }
}
