using System.Windows.Data;
using System.Windows.Markup;

namespace CommandForge.Wpf.Resources;

/// <summary>
/// XAML markup extension that resolves a resource key to its localized string, e.g.
/// <c>Text="{res:Loc Detail_Copy}"</c>. Returns a one-way <see cref="Binding"/> to
/// <see cref="LocalizationManager"/>'s indexer so the text updates live when the culture changes.
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

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}
