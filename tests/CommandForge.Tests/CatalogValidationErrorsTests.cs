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

    [Fact]
    public void Build_MaxOsBuildBelowMinOsBuild_ProducesError()
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
                MinOsBuild = 22000,
                MaxOsBuild = 19045,
            },
        };

        var result = CatalogLoader.Build(categories, commands);

        Assert.Contains(result.Errors, e => e.Contains("c1", StringComparison.Ordinal) && e.Contains("maxOsBuild", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_NonPositiveOsBuild_ProducesError()
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
                MinOsBuild = 0,
            },
        };

        var result = CatalogLoader.Build(categories, commands);

        Assert.Contains(result.Errors, e => e.Contains("c1", StringComparison.Ordinal) && e.Contains("minOsBuild", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_ValidOsBuildRange_HasNoErrors()
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
                MinOsBuild = 22000,
            },
        };

        var result = CatalogLoader.Build(categories, commands);

        Assert.Empty(result.Errors);
        Assert.Equal(22000, result.Commands[0].MinOsBuild);
    }
}
