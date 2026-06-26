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
    public void SystemCulture_IsStable_AfterCultureChange()
    {
        var original = LocalizationManager.Instance.CurrentCulture;
        try
        {
            // "Follow OS" must keep resolving to the OS culture, not the last-selected language.
            var systemBefore = LocalizationManager.Instance.SystemCulture.Name;
            LocalizationManager.Instance.SetCulture(new CultureInfo("vi"));
            Assert.Equal(systemBefore, LocalizationManager.Instance.SystemCulture.Name);
        }
        finally
        {
            LocalizationManager.Instance.SetCulture(original);
        }
    }

    [Theory]
    [InlineData("vi")]
    [InlineData("ja")]
    [InlineData("zh-Hans")]
    [InlineData("es")]
    public void SetCulture_LoadsSatellite_NotEnglishFallback(string culture)
    {
        var original = LocalizationManager.Instance.CurrentCulture;
        try
        {
            LocalizationManager.Instance.SetCulture(new CultureInfo("en"));
            var en = LocalizationManager.Instance["Menu_CheckForUpdates"];

            LocalizationManager.Instance.SetCulture(new CultureInfo(culture));
            var translated = LocalizationManager.Instance["Menu_CheckForUpdates"];

            Assert.False(string.IsNullOrEmpty(translated));
            // A real satellite resolves to a translated string; an English fallback would be identical.
            Assert.NotEqual(en, translated);
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
