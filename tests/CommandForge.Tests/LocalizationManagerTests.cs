using System.ComponentModel;
using System.Globalization;
using CommandForge.Wpf.Resources;

namespace CommandForge.Tests;

/// <summary>Verifies the live-localization source flips strings and notifies bindings on culture change.</summary>
public sealed class LocalizationManagerTests
{
    [Fact]
    public void SetCulture_FlipsIndexerLanguage()
    {
        var original = LocalizationManager.Instance.CurrentCulture;
        try
        {
            LocalizationManager.Instance.SetCulture(new CultureInfo("en"));
            var en = LocalizationManager.Instance["Menu_CheckForUpdates"];

            LocalizationManager.Instance.SetCulture(new CultureInfo("vi"));
            var vi = LocalizationManager.Instance["Menu_CheckForUpdates"];

            Assert.False(string.IsNullOrEmpty(en));
            Assert.False(string.IsNullOrEmpty(vi));
            Assert.NotEqual(en, vi); // EN and VI resources differ for this key
        }
        finally
        {
            LocalizationManager.Instance.SetCulture(original);
        }
    }

    [Fact]
    public void SetCulture_RaisesItemBracketPropertyChanged()
    {
        var original = LocalizationManager.Instance.CurrentCulture;
        try
        {
            LocalizationManager.Instance.SetCulture(new CultureInfo("en"));

            string? changed = null;
            void Handler(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Item[]")
                {
                    changed = e.PropertyName;
                }
            }

            LocalizationManager.Instance.PropertyChanged += Handler;
            LocalizationManager.Instance.SetCulture(new CultureInfo("vi"));
            LocalizationManager.Instance.PropertyChanged -= Handler;

            Assert.Equal("Item[]", changed);
        }
        finally
        {
            LocalizationManager.Instance.SetCulture(original);
        }
    }
}
