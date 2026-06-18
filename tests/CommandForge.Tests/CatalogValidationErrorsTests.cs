using CommandForge.Infrastructure.Catalog;

namespace CommandForge.Tests;

/// <summary>Verifies catalog validation surfaces errors (the data behind the Debug panel's Catalog tab).</summary>
public sealed class CatalogValidationErrorsTests
{
    [Fact]
    public void Build_UnknownCategoryReference_ProducesError()
    {
        var categories = new[] { new CategoryDto { Id = "cat", TitleKey = "Cat_K" } };
        var commands = new[]
        {
            new CommandDto
            {
                Id = "c1",
                CategoryId = "does-not-exist",
                TitleKey = "T",
                DescriptionKey = "D",
                Executable = "cmd",
            },
        };

        var result = CatalogLoader.Build(categories, commands);

        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("does-not-exist", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_ValidCatalog_HasNoErrors()
    {
        var categories = new[] { new CategoryDto { Id = "cat", TitleKey = "Cat_K" } };
        var commands = new[]
        {
            new CommandDto
            {
                Id = "c1",
                CategoryId = "cat",
                TitleKey = "T",
                DescriptionKey = "D",
                Executable = "cmd",
            },
        };

        var result = CatalogLoader.Build(categories, commands);

        Assert.Empty(result.Errors);
    }
}
