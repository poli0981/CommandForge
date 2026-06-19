namespace CommandForge.Wpf.ViewModels;

/// <summary>The active section of the main shell content area.</summary>
public enum ShellSection
{
    /// <summary>The Home dashboard (recent commands, favorites, system status).</summary>
    Home,

    /// <summary>The command catalog (browse/search/run).</summary>
    Catalog,

    /// <summary>The execution-history screen (past runs, re-run).</summary>
    History,

    /// <summary>The recipes screen (build/run command chains).</summary>
    Recipes,

    /// <summary>The Settings screen.</summary>
    Settings,

    /// <summary>The Log Viewer.</summary>
    LogViewer,

    /// <summary>The Debug panel.</summary>
    Debug,
}
