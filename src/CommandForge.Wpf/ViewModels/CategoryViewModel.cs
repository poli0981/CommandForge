using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommandForge.Wpf.ViewModels;

/// <summary>A sidebar category entry. <see cref="Id"/> is <see langword="null"/> for the "All" pseudo-category.</summary>
public sealed partial class CategoryViewModel : ObservableObject
{
    private readonly string _titleKey;

    public CategoryViewModel(string? id, string titleKey, string? icon, int count)
    {
        Id = id;
        _titleKey = titleKey;
        _title = Strings.Get(titleKey);
        Icon = icon;
        Count = count;
    }

    public string? Id { get; }

    [ObservableProperty]
    private string _title;

    public string? Icon { get; }

    public int Count { get; }

    /// <summary>Re-resolves the localized title (after a culture change).</summary>
    public void Refresh() => Title = Strings.Get(_titleKey);
}
