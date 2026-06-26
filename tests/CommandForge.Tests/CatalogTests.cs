using CommandForge.Domain;
using CommandForge.Infrastructure.Catalog;

namespace CommandForge.Tests;

/// <summary>Validates the embedded command catalog (structural integrity).</summary>
public sealed class CatalogTests
{
    private static readonly CatalogLoadResult Catalog = CatalogLoader.LoadEmbedded();

    [Fact]
    public void Catalog_Loads_WithoutErrors()
    {
        Assert.Empty(Catalog.Errors);
    }

    [Fact]
    public void Catalog_HasCategoriesAndCommands()
    {
        Assert.Equal(11, Catalog.Categories.Count);
        Assert.True(Catalog.Commands.Count >= 20, $"Expected >= 20 commands, got {Catalog.Commands.Count}.");
    }

    [Fact]
    public void Catalog_HasStorageCategory_WithCommands()
    {
        Assert.Contains(Catalog.Categories, c => c.Id == "storage");
        Assert.Contains(Catalog.Commands, c => c.CategoryId == "storage");
    }

    [Fact]
    public void Catalog_DangerousCommands_RequireConfirmation()
    {
        // Golden rule: Dangerous commands must always confirm before running.
        var offenders = Catalog.Commands
            .Where(c => c.DangerLevel == DangerLevel.Dangerous && !c.ConfirmBeforeRun)
            .Select(c => c.Id)
            .ToList();
        Assert.True(offenders.Count == 0, $"Dangerous commands missing confirmation: {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Catalog_AllCommandIds_AreUnique()
    {
        var ids = Catalog.Commands.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Catalog_EveryCommand_HasKnownCategory()
    {
        var categoryIds = Catalog.Categories.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        Assert.All(Catalog.Commands, c => Assert.Contains(c.CategoryId, categoryIds));
    }

    [Fact]
    public void Catalog_RevertRefs_PointToExistingCommands()
    {
        var ids = Catalog.Commands.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var command in Catalog.Commands.Where(c => c.RevertCommandId is not null))
        {
            Assert.Contains(command.RevertCommandId!, ids);
        }
    }
}
