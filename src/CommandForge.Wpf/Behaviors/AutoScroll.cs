using System.Windows;
using System.Windows.Controls;

namespace CommandForge.Wpf.Behaviors;

/// <summary>
/// Attached behavior that keeps a <see cref="ScrollViewer"/> pinned to the bottom as content
/// grows — but only while the user is already at the bottom (scrolling up pauses auto-follow).
/// </summary>
public static class AutoScroll
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(AutoScroll), new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);

    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
        {
            return;
        }

        if (e.NewValue is true)
        {
            scrollViewer.ScrollChanged += OnScrollChanged;
        }
        else
        {
            scrollViewer.ScrollChanged -= OnScrollChanged;
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange <= 0)
        {
            return;
        }

        var scrollViewer = (ScrollViewer)sender;
        var heightBeforeChange = e.ExtentHeight - e.ExtentHeightChange;
        var wasAtBottom = e.VerticalOffset + e.ViewportHeight >= heightBeforeChange - 1.0;
        if (wasAtBottom)
        {
            scrollViewer.ScrollToEnd();
        }
    }
}
