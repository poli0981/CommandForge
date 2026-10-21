using CommandForge.Desktop.Resources;

namespace CommandForge.Desktop.ViewModels;

/// <summary>A selectable UI language. <see cref="Code"/> is <c>""</c> for "follow OS".</summary>
public sealed record LanguageOption(string Code, string DisplayName)
{
    /// <summary>"Follow Windows" plus every shipped language, each named in its own language.</summary>
    public static IReadOnlyList<LanguageOption> CreateAll() =>
    [
        new LanguageOption(string.Empty, Strings.Get("LanguageOption_System")),
        new LanguageOption("en", "English"),
        new LanguageOption("vi", "Tiếng Việt"),
        new LanguageOption("ja", "日本語"),
        new LanguageOption("zh-Hans", "简体中文"),
        new LanguageOption("es", "Español"),
    ];
}
