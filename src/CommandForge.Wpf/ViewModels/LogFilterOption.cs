namespace CommandForge.Wpf.ViewModels;

/// <summary>A Log Viewer level-filter option (value + localized label) for the dropdown.</summary>
public sealed record LogFilterOption(LogLevelFilter Value, string DisplayName);
