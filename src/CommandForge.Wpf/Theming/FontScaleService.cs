using CommandForge.Application.Settings;

namespace CommandForge.Wpf.Theming;

/// <summary>
/// <see cref="IFontScaleService"/> that writes the root font size into the app-level
/// <see cref="ResourceKey"/> resource. Windows bind <c>TextElement.FontSize</c> to it as a
/// DynamicResource, so inherited text rescales live.
/// </summary>
public sealed class FontScaleService : IFontScaleService
{
    /// <summary>The <see cref="System.Windows.Application.Resources"/> key holding the root font size (a <see cref="double"/>).</summary>
    public const string ResourceKey = "AppFontSize";

    /// <inheritdoc />
    public void Apply(FontScale scale)
    {
        double size = scale switch
        {
            FontScale.Small => 12d,
            FontScale.Large => 15d,
            _ => 13d,
        };

        if (System.Windows.Application.Current is { } app)
        {
            app.Resources[ResourceKey] = size;
        }
    }
}
