using System.Windows.Markup;

namespace CommandForge.Wpf.Resources;

/// <summary>
/// XAML markup extension that resolves a resource key to its localized string, e.g.
/// <c>Text="{res:Loc Detail_Copy}"</c>. Resolves once at load (Phase 0/1 set culture at startup).
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key) => Key = key;

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) => Strings.Get(Key);
}
