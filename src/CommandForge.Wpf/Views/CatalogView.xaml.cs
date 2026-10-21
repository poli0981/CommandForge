using System.Windows.Controls;

namespace CommandForge.Desktop.Views;

/// <summary>The catalog browse/run page: search and risk filters, command list, detail and console.</summary>
public partial class CatalogView : UserControl
{
    public CatalogView() => InitializeComponent();

    /// <summary>Puts the cursor in the search box (Ctrl+F, or after navigating here from the search box).</summary>
    public void FocusSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }
}
