namespace CommandForge.Wpf.ViewModels;

/// <summary>A selectable UI language. <see cref="Code"/> is <c>""</c> for "follow OS".</summary>
public sealed record LanguageOption(string Code, string DisplayName);
