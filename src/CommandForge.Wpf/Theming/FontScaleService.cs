using CommandForge.Application.Settings;

namespace CommandForge.Desktop.Theming;

/// <summary>
/// <see cref="IFontScaleService"/> that rewrites the app-level font size resources. Text binds to
/// them as DynamicResource (<c>AppFontSize</c> and its caption/subtitle/title/console companions),
/// and WPF-UI controls read <c>ControlContentThemeFontSize</c>, so the whole UI rescales live.
/// </summary>
public sealed class FontScaleService : IFontScaleService
{
    /// <summary>The <see cref="System.Windows.Application.Resources"/> key holding the body font size (a <see cref="double"/>).</summary>
    public const string ResourceKey = "AppFontSize";

    /// <inheritdoc />
    public void Apply(FontScale scale)
    {
        double body = scale switch
        {
            FontScale.Small => 12d,
            FontScale.Large => 16d,
            _ => 14d,
        };

        if (System.Windows.Application.Current is not { } app)
        {
            return;
        }

        var resources = app.Resources;
        resources[ResourceKey] = body;
        resources["ControlContentThemeFontSize"] = body;
        resources["AppFontSizeCaption"] = body - 2;
        resources["AppFontSizeSubtitle"] = Math.Round(body * 10 / 7);
        resources["AppFontSizeTitle"] = body * 2;
        resources["AppFontSizeConsole"] = body - 1.5;
    }
}
