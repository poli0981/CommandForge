using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CommandForge.Wpf.Behaviors;

/// <summary>
/// Attached behavior that renders a <see cref="TextBlock"/>'s text with the fuzzy-matched
/// characters bolded. Bind <see cref="TextProperty"/> to the text and
/// <see cref="MatchesProperty"/> to the matched indices.
/// </summary>
public static class TextHighlight
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(TextHighlight), new PropertyMetadata(string.Empty, OnChanged));

    public static readonly DependencyProperty MatchesProperty = DependencyProperty.RegisterAttached(
        "Matches", typeof(IReadOnlyList<int>), typeof(TextHighlight), new PropertyMetadata(null, OnChanged));

    public static string GetText(DependencyObject target) => (string)target.GetValue(TextProperty);

    public static void SetText(DependencyObject target, string value) => target.SetValue(TextProperty, value);

    public static IReadOnlyList<int>? GetMatches(DependencyObject target)
        => (IReadOnlyList<int>?)target.GetValue(MatchesProperty);

    public static void SetMatches(DependencyObject target, IReadOnlyList<int>? value)
        => target.SetValue(MatchesProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
        {
            return;
        }

        var text = GetText(textBlock) ?? string.Empty;
        var matches = GetMatches(textBlock);
        textBlock.Inlines.Clear();

        if (matches is null || matches.Count == 0)
        {
            textBlock.Inlines.Add(new Run(text));
            return;
        }

        var matched = new HashSet<int>(matches);
        var index = 0;
        while (index < text.Length)
        {
            var isMatch = matched.Contains(index);
            var start = index;
            while (index < text.Length && matched.Contains(index) == isMatch)
            {
                index++;
            }

            var run = new Run(text[start..index]);
            if (isMatch)
            {
                run.FontWeight = FontWeights.Bold;
            }

            textBlock.Inlines.Add(run);
        }
    }
}
