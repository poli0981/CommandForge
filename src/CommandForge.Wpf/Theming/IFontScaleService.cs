using CommandForge.Application.Settings;

namespace CommandForge.Wpf.Theming;

/// <summary>Applies the UI font size at runtime, with no restart.</summary>
public interface IFontScaleService
{
    /// <summary>Applies <paramref name="scale"/> by updating the app-level <c>AppFontSize</c> resource.</summary>
    public void Apply(FontScale scale);
}
