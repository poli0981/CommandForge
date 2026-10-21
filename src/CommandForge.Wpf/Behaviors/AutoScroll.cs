using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CommandForge.Desktop.Behaviors;

/// <summary>
/// Attached behavior that keeps a scrolling view pinned to the bottom as content grows — but only
/// while the user is at the bottom (scrolling up pauses auto-follow; scrolling back down resumes it).
/// Works on a <see cref="ScrollViewer"/> or on a control that hosts one, such as a <see cref="ListBox"/>.
/// </summary>
public static class AutoScroll
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(AutoScroll), new PropertyMetadata(false, OnEnabledChanged));

    private static readonly DependencyProperty IsAtBottomProperty = DependencyProperty.RegisterAttached(
        "IsAtBottom", typeof(bool), typeof(AutoScroll), new PropertyMetadata(true));

    private static readonly DependencyProperty InnerScrollViewerProperty = DependencyProperty.RegisterAttached(
        "InnerScrollViewer", typeof(ScrollViewer), typeof(AutoScroll), new PropertyMetadata(null));

    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);

    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var enabled = e.NewValue is true;
        if (d is ScrollViewer scrollViewer)
        {
            Attach(scrollViewer, enabled);
            return;
        }

        if (d is not FrameworkElement host)
        {
            return;
        }

        // A ListBox creates its ScrollViewer from its template, so attach once it is loaded.
        if (host.GetValue(InnerScrollViewerProperty) is ScrollViewer inner)
        {
            Attach(inner, enabled);
        }
        else if (enabled)
        {
            host.Loaded -= OnHostLoaded;
            host.Loaded += OnHostLoaded;
        }
    }

    private static void OnHostLoaded(object sender, RoutedEventArgs e)
    {
        var host = (FrameworkElement)sender;
        host.Loaded -= OnHostLoaded;
        if (FindScrollViewer(host) is { } inner)
        {
            host.SetValue(InnerScrollViewerProperty, inner);
            Attach(inner, GetEnabled(host));
        }
    }

    private static void Attach(ScrollViewer scrollViewer, bool enabled)
    {
        scrollViewer.ScrollChanged -= OnScrollChanged;
        if (enabled)
        {
            scrollViewer.ScrollChanged += OnScrollChanged;
            scrollViewer.ScrollToEnd();
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var scrollViewer = (ScrollViewer)sender;

        // Everything fits (e.g. the output was just cleared for a new run): follow from now on.
        if (e.ExtentHeight <= e.ViewportHeight)
        {
            scrollViewer.SetValue(IsAtBottomProperty, true);
            return;
        }

        if (e.ExtentHeightChange == 0)
        {
            // The user scrolled or the view resized: remember whether they are at the bottom.
            scrollViewer.SetValue(IsAtBottomProperty, e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 1.0);
            return;
        }

        // The content changed (grew, or was trimmed): keep following if the user was at the bottom.
        if ((bool)scrollViewer.GetValue(IsAtBottomProperty))
        {
            scrollViewer.ScrollToEnd();
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            if (FindScrollViewer(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}
