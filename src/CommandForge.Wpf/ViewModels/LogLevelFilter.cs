namespace CommandForge.Wpf.ViewModels;

/// <summary>The Log Viewer's level filter (the "Level" dropdown). Distinct from the 6-value <c>LogLevel</c>.</summary>
public enum LogLevelFilter
{
    /// <summary>Show all levels.</summary>
    All,

    /// <summary>Information only.</summary>
    Information,

    /// <summary>Warning only.</summary>
    Warning,

    /// <summary>Error and Fatal.</summary>
    Error,

    /// <summary>Debug and Verbose.</summary>
    Debug,
}
