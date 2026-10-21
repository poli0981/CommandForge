using CommandForge.Domain;
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

    [Theory]
    [InlineData("cmd /c del")]
    [InlineData(@"C:\Tools\tool.exe")]
    [InlineData("\"reg\"")]
    public void Build_ExecutableThatIsNotAPlainName_IsRejected(string executable)
        => AssertRejected(Valid() with { Executable = executable }, "plain program name");

    [Fact]
    public void Build_UnknownArgumentToken_IsRejected()
        => AssertRejected(Valid() with { ArgsTemplate = @"{ProgramFiles}\tool" }, "{ProgramFiles}");

    [Theory]
    [InlineData(DangerLevel.Caution)]
    [InlineData(DangerLevel.Dangerous)]
    public void Build_RiskyCommandWithoutConfirmation_IsRejected(DangerLevel level)
        => AssertRejected(Valid() with { DangerLevel = level, ConfirmBeforeRun = false }, "confirmBeforeRun");

    [Theory]
    [InlineData(RestartPolicy.FromExitCode)]
    [InlineData(RestartPolicy.FromOutputRegex)]
    public void Build_LaunchModeWithCapturedRestartPolicy_IsRejected(RestartPolicy restart)
        => AssertRejected(Valid() with { ExecutionMode = ExecutionMode.Launch, Restart = restart, RestartRegex = "restart" }, "Launch mode");

    [Fact]
    public void Build_RestartRegexWithAnotherPolicy_IsRejected()
        => AssertRejected(Valid() with { Restart = RestartPolicy.FromExitCode, RestartRegex = "reboot" }, "restartRegex");

    [Fact]
    public void Build_InvalidRestartRegex_IsRejected()
        => AssertRejected(Valid() with { Restart = RestartPolicy.FromOutputRegex, RestartRegex = "(unclosed" }, "invalid restartRegex");

    [Fact]
    public void Build_DuplicateId_KeepsTheFirstAndReportsTheSecond()
    {
        var result = CatalogLoader.Build(Categories, [Valid(), Valid() with { TitleKey = "Other" }]);

        var command = Assert.Single(result.Commands);
        Assert.Equal("T", command.TitleKey);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate command id 'c1'", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_RevertToARejectedCommand_ClearsTheRevert()
    {
        // c2 is rejected, so c1 must not offer an Undo that points at it.
        var result = CatalogLoader.Build(
            Categories,
            [Valid() with { RevertCommandId = "c2" }, Valid() with { Id = "c2", ArgsTemplate = "{Nope}" }]);

        var command = Assert.Single(result.Commands);
        Assert.Null(command.RevertCommandId);
        Assert.Contains(result.Errors, e => e.Contains("revertCommandId 'c2'", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_CommandInARejectedCategory_IsRejected()
    {
        var result = CatalogLoader.Build([new CategoryDto { Id = "cat" }], [Valid()]);

        Assert.Empty(result.Categories);
        Assert.Empty(result.Commands);
        Assert.Contains(result.Errors, e => e.Contains("unknown categoryId 'cat'", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_SafeCommandWithKnownTokens_IsAccepted()
    {
        var result = CatalogLoader.Build(Categories, [Valid() with { ArgsTemplate = "{SystemDrive} /scan", Restart = RestartPolicy.FromExitCode }]);

        Assert.Empty(result.Errors);
        Assert.Single(result.Commands);
    }

    private static CategoryDto[] Categories => [new CategoryDto { Id = "cat", TitleKey = "Cat_K" }];

    private static CommandDto Valid() => new()
    {
        Id = "c1",
        CategoryId = "cat",
        TitleKey = "T",
        DescriptionKey = "D",
        Executable = "chkdsk",
    };

    private static void AssertRejected(CommandDto dto, string expectedFragment)
    {
        var result = CatalogLoader.Build(Categories, [dto]);

        Assert.Empty(result.Commands);
        Assert.Contains(result.Errors, e => e.Contains(expectedFragment, StringComparison.Ordinal));
    }
}
