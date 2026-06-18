namespace CommandForge.Wpf.ViewModels;

/// <summary>The active section of the main shell content area. Home is added in Phase 5c.</summary>
public enum ShellSection
{
    /// <summary>The command catalog (browse/search/run).</summary>
    Catalog,

    /// <summary>The Settings screen.</summary>
    Settings,

    /// <summary>The Log Viewer.</summary>
    LogViewer,

    /// <summary>The Debug panel.</summary>
    Debug,
}
