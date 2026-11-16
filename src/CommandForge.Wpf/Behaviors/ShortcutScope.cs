using System.Windows;
using System.Windows.Controls;

namespace CommandForge.Desktop.Behaviors;

/// <summary>
/// Where the window's own shortcuts may fire. Ctrl+Q (exit) does nothing while the keyboard is in a
/// text box being edited, so a slip of the finger can't discard what is being typed; a search box
/// opts back in with <c>beh:ShortcutScope.AllowExit="True"</c>, since its text is cheap to retype.
/// </summary>
public static class ShortcutScope
{
    public static readonly DependencyProperty AllowExitProperty = DependencyProperty.RegisterAttached(
        "AllowExit", typeof(bool), typeof(ShortcutScope), new PropertyMetadata(false));

    public static bool GetAllowExit(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(AllowExitProperty);
    }

    public static void SetAllowExit(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(AllowExitProperty, value);
    }

    /// <summary>Whether Ctrl+Q may close the window with <paramref name="focused"/> holding the keyboard.</summary>
    public static bool CanExitFrom(IInputElement? focused) => focused switch
    {
        TextBox { IsReadOnly: false } box => GetAllowExit(box) || FindTemplatedTextBox(box) is { } owner && GetAllowExit(owner),
        _ => true,
    };

    // WPF-UI's TextBox is the TextBox itself; a plain TextBox inside another control's template
    // (an editable ComboBox) answers for its templated parent.
    private static DependencyObject? FindTemplatedTextBox(TextBox box) => box.TemplatedParent;
}
