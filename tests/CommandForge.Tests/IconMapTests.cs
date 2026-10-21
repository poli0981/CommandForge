using CommandForge.Desktop.Converters;
using CommandForge.Infrastructure.Catalog;
using Wpf.Ui.Controls;

namespace CommandForge.Tests;

/// <summary>Every icon the catalog names has a Fluent icon of its own (never the fallback).</summary>
public sealed class IconMapTests
{
    private static readonly CatalogLoadResult Catalog = CatalogLoader.LoadEmbedded();

    [Fact]
    public void EveryCommandIcon_IsMapped()
    {
        var unmapped = Catalog.Commands
            .Where(c => !IconMap.IsMapped(c.Icon))
            .Select(c => $"{c.Id} ({c.Icon ?? "no icon"})")
            .ToList();

        Assert.True(unmapped.Count == 0, $"Commands without a Fluent icon: {string.Join(", ", unmapped)}.");
    }

    [Fact]
    public void EveryCategoryIcon_IsMapped()
        => Assert.All(Catalog.Categories, c => Assert.True(IconMap.IsMapped(c.Icon), $"Category {c.Id} icon {c.Icon} is not mapped."));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NoSuchIcon")]
    public void UnknownNames_UseTheFallback(string? name)
        => Assert.Equal(IconMap.Fallback, IconMap.Resolve(name));

    [Fact]
    public void Names_AreCaseInsensitive()
        => Assert.Equal(SymbolRegular.Broom24, IconMap.Resolve("broom"));
}
