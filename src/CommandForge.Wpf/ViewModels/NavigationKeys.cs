namespace CommandForge.Desktop.ViewModels;

/// <summary>
/// Stable keys for the shell's navigation targets. The main window tags each navigation item with
/// one, and <see cref="MainViewModel.NavigationKey"/> / <see cref="MainViewModel.Navigate"/> speak
/// the same language, so the pane selection and the view-model state stay in sync both ways.
/// </summary>
public static class NavigationKeys
{
    public const string Home = "home";
    public const string Favorites = "favorites";
    public const string Catalog = "catalog";
    public const string History = "history";
    public const string Recipes = "recipes";
    public const string UserCommands = "user-commands";
    public const string Settings = "settings";
    public const string Logs = "logs";
    public const string Debug = "debug";

    private const string CategoryPrefix = "category:";

    /// <summary>The key of one catalog category.</summary>
    public static string Category(string categoryId) => CategoryPrefix + categoryId;

    /// <summary>The category id in a category key, or <see langword="null"/> for any other key.</summary>
    public static string? CategoryIdOf(string key)
        => key.StartsWith(CategoryPrefix, StringComparison.Ordinal) ? key[CategoryPrefix.Length..] : null;
}
