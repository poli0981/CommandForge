using CommandForge.Application.Search;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>Tests for fuzzy matching and the command search use-case.</summary>
public sealed class SearchTests
{
    private static SearchableCommand Make(
        string id,
        string title,
        string description,
        string category = "cat",
        bool admin = false)
    {
        var command = new CommandDefinition
        {
            Id = id,
            CategoryId = category,
            TitleKey = "t",
            DescriptionKey = "d",
            Executable = "x",
            RequiresAdmin = admin,
        };
        return new SearchableCommand(command, title, description, []);
    }

    [Fact]
    public void Fuzzy_ReturnsNull_WhenNotSubsequence()
        => Assert.Null(FuzzyMatcher.Match("System", "xyz"));

    [Fact]
    public void Fuzzy_Matches_Subsequence()
    {
        var match = FuzzyMatcher.Match("System information", "sys");

        Assert.NotNull(match);
        Assert.Equal(3, match!.Value.MatchedIndices.Count);
    }

    [Fact]
    public void Fuzzy_EmptyQuery_ScoresZero()
    {
        var match = FuzzyMatcher.Match("anything", string.Empty);

        Assert.NotNull(match);
        Assert.Equal(0, match!.Value.Score);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAllInOrder()
    {
        var items = new[] { Make("a", "Alpha", "x"), Make("b", "Beta", "y") };

        var hits = SearchCommandsUseCase.Search(items, string.Empty);

        Assert.Equal(["a", "b"], hits.Select(h => h.Command.Id));
    }

    [Fact]
    public void Search_FiltersByQuery()
    {
        var items = new[] { Make("a", "Flush DNS", "clear dns cache"), Make("b", "Battery report", "power") };

        var hits = SearchCommandsUseCase.Search(items, "dns");

        Assert.Single(hits);
        Assert.Equal("a", hits[0].Command.Id);
    }

    [Fact]
    public void Search_FiltersByCategory()
    {
        var items = new[] { Make("a", "A", "x", category: "network"), Make("b", "B", "y", category: "power") };

        var hits = SearchCommandsUseCase.Search(items, string.Empty, new CommandSearchFilter { CategoryId = "power" });

        Assert.Single(hits);
        Assert.Equal("b", hits[0].Command.Id);
    }

    [Fact]
    public void Search_FiltersByRequiresAdmin()
    {
        var items = new[] { Make("a", "A", "x", admin: true), Make("b", "B", "y", admin: false) };

        var hits = SearchCommandsUseCase.Search(items, string.Empty, new CommandSearchFilter { RequiresAdmin = true });

        Assert.Single(hits);
        Assert.Equal("a", hits[0].Command.Id);
    }

    [Fact]
    public void Search_RanksTitleMatch_AboveDescriptionMatch()
    {
        var items = new[]
        {
            Make("desc", "Zzz", "this contains flush somewhere"),
            Make("title", "Flush DNS", "unrelated text"),
        };

        var hits = SearchCommandsUseCase.Search(items, "flush");

        Assert.Equal("title", hits[0].Command.Id);
    }
}
