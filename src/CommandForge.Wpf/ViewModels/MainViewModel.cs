using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// View-model for the main shell. Phase 0 only owns the sidebar collapse state;
/// catalog, search and execution arrive in later phases.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSidebarCollapsed;

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;
}
