using CommandForge.Application.Ports;
using CommandForge.Application.Search;
using CommandForge.Wpf.Resources;

namespace CommandForge.Wpf.ViewModels;

/// <summary>View-model projections of the catalog, with resource keys resolved to localized text.</summary>
internal sealed record CatalogViewModels(
    IReadOnlyList<CommandItemViewModel> Items,
    IReadOnlyList<SearchableCommand> Searchable,
    IReadOnlyDictionary<string, string> CategoryTitles);

/// <summary>
/// Builds fresh <see cref="CommandItemViewModel"/> + <see cref="SearchableCommand"/> lists from the
/// catalog, resolving title/description keys to text once. Each caller gets its own instances so
/// highlight state never leaks between the main list and the palette.
/// </summary>
internal static class CatalogViewModelBuilder
{
    /// <summary>
    /// Projects the catalog to view-models, hiding commands that do not apply to the current OS build
    /// (e.g. Windows 11-only tweaks on Windows 10). The hidden commands never reach the list, palette,
    /// category counts or any dashboard.
    /// </summary>
    public static CatalogViewModels Build(ICatalogProvider catalog, int currentOsBuild)
    {
        var categoryTitles = catalog.GetCategories()
            .ToDictionary(c => c.Id, c => Strings.Get(c.TitleKey), StringComparer.Ordinal);

        var items = new List<CommandItemViewModel>();
        var searchable = new List<SearchableCommand>();
        foreach (var command in catalog.GetCommands())
        {
            if (!command.AppliesToOsBuild(currentOsBuild))
            {
                continue;
            }

            var title = Strings.Get(command.TitleKey);
            var description = Strings.Get(command.DescriptionKey);
            items.Add(new CommandItemViewModel(command, title, description));
            searchable.Add(new SearchableCommand(command, title, description, command.Tags));
        }

        return new CatalogViewModels(items, searchable, categoryTitles);
    }
}
