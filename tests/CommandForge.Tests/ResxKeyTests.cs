using System.Globalization;
using System.Resources;
using CommandForge.Infrastructure.Catalog;
using CommandForge.Wpf.Resources;

namespace CommandForge.Tests;

/// <summary>
/// Cross-checks that every catalog title/description resource key exists in both the neutral
/// (EN) resources and the Vietnamese satellite (no parent fallback).
/// </summary>
public sealed class ResxKeyTests
{
    private static readonly CatalogLoadResult Catalog = CatalogLoader.LoadEmbedded();

    [Fact]
    public void Catalog_AllKeys_ExistInResx_EN_and_VI()
    {
        var manager = new ResourceManager("CommandForge.Wpf.Resources.Strings", typeof(Strings).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        var vietnamese = manager.GetResourceSet(new CultureInfo("vi"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(english);
        Assert.NotNull(vietnamese);

        var keys = new List<string>();
        keys.AddRange(Catalog.Categories.Select(c => c.TitleKey));
        keys.AddRange(Catalog.Commands.Select(c => c.TitleKey));
        keys.AddRange(Catalog.Commands.Select(c => c.DescriptionKey));

        foreach (var key in keys)
        {
            Assert.True(english!.GetString(key) is not null, $"Missing EN resource for key '{key}'.");
            Assert.True(vietnamese!.GetString(key) is not null, $"Missing VI resource for key '{key}'.");
        }
    }
}
