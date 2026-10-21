using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using CommandForge.Desktop.Resources;
using CommandForge.Domain;

namespace CommandForge.Desktop.Views;

/// <summary>A small chip showing a command's risk level (icon, localized text and color).</summary>
public partial class DangerBadge : UserControl
{
    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(
        nameof(Level), typeof(DangerLevel), typeof(DangerBadge), new PropertyMetadata(DangerLevel.Safe, OnLevelChanged));

    public DangerBadge()
    {
        InitializeComponent();
        UpdateText();

        // Subscribe only while shown: the static event would otherwise keep every badge alive.
        Loaded += (_, _) =>
        {
            LocalizationManager.Instance.CultureChanged += OnCultureChanged;
            UpdateText();
        };
        Unloaded += (_, _) => LocalizationManager.Instance.CultureChanged -= OnCultureChanged;
    }

    public DangerLevel Level
    {
        get => (DangerLevel)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DangerBadge)d).UpdateText();

    private void OnCultureChanged(object? sender, EventArgs e) => UpdateText();

    private void UpdateText()
    {
        var text = Strings.Get($"Danger_{Level}");
        LabelText.Text = text;
        AutomationProperties.SetName(this, text);
    }
}
