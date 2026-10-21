using System.Windows.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>The console shared by the catalog, recipes and user-command pages (DataContext: ExecutionViewModel).</summary>
public partial class ConsoleView : UserControl
{
    public ConsoleView() => InitializeComponent();
}
