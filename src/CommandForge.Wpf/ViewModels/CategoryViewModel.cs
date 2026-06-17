namespace CommandForge.Wpf.ViewModels;

/// <summary>A sidebar category entry. <see cref="Id"/> is <see langword="null"/> for the "All" pseudo-category.</summary>
public sealed class CategoryViewModel
{
    public CategoryViewModel(string? id, string title, string? icon, int count)
    {
        Id = id;
        Title = title;
        Icon = icon;
        Count = count;
    }

    public string? Id { get; }

    public string Title { get; }

    public string? Icon { get; }

    public int Count { get; }
}
