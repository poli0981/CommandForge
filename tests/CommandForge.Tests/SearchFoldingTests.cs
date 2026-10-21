using CommandForge.Application.Search;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>Tests accent-insensitive matching, precise description/tag matching and highlighting.</summary>
public sealed class SearchFoldingTests
{
    [Theory]
    [InlineData("Dọn dẹp ổ đĩa", "don dep o dia")]
    [InlineData("Đặt lại mạng", "dat lai mang")]
    [InlineData("CỬA SỔ", "cua so")]
    [InlineData("Configuración", "configuracion")]
    public void Fold_RemovesAccentsAndCase(string text, string expected)
        => Assert.Equal(expected, TextFolding.Fold(text));

    [Fact]
    public void Fold_KeepsEveryIndex()
    {
        // Highlight indices are computed on the folded text and drawn on the original.
        const string text = "Xóa bộ nhớ đệm DNS — Ổn định";
        Assert.Equal(text.Length, TextFolding.Fold(text).Length);
    }

    [Fact]
    public void Search_IgnoresVietnameseAccents()
    {
        var hits = SearchCommandsUseCase.Search([Make("vi", "Dọn dẹp ổ đĩa", "Xóa tệp tạm")], "don dep");

        Assert.Equal("vi", Assert.Single(hits).Command.Id);
    }

    [Fact]
    public void Search_DescriptionNeedsTheWordItself_NotScatteredLetters()
    {
        // "dns" is a subsequence of "Delete ... and system caches"; that is not a match.
        var items = new[]
        {
            Make("cleanup", "Disk Cleanup", "Delete temporary files and system caches"),
            Make("flush", "Flush resolver", "Clear the DNS client cache"),
        };

        var hits = SearchCommandsUseCase.Search(items, "dns");

        Assert.Equal("flush", Assert.Single(hits).Command.Id);
    }

    [Fact]
    public void Search_TagsMatchByWordPrefix()
    {
        var items = new[]
        {
            Make("wifi", "Connection details", "x", tags: ["wifi", "signal"]),
            Make("other", "Other", "x", tags: ["swift"]),
        };

        Assert.Equal("wifi", Assert.Single(SearchCommandsUseCase.Search(items, "wi")).Command.Id);
    }

    [Fact]
    public void Search_HighlightsTheMatchedTitleCharacters()
    {
        var hit = Assert.Single(SearchCommandsUseCase.Search([Make("f", "Flush DNS cache", "x")], "dns"));

        Assert.Equal([6, 7, 8], hit.TitleMatches);
    }

    [Fact]
    public void Search_PrefersAWordStartMatch()
    {
        var items = new[] { Make("inside", "Autoflush", "x"), Make("start", "Flush DNS", "x") };

        var hits = SearchCommandsUseCase.Search(items, "flush");

        Assert.Equal(["start", "inside"], hits.Select(h => h.Command.Id));
    }

    [Fact]
    public void Search_EqualScores_KeepCatalogOrder()
    {
        var items = new[] { Make("b", "Restart service B", "x"), Make("a", "Restart service A", "x") };

        Assert.Equal(["b", "a"], SearchCommandsUseCase.Search(items, "restart").Select(h => h.Command.Id));
    }

    [Fact]
    public void RequiresRestartFilter_MeansAlways()
    {
        var always = new SearchableCommand(Command("always") with { Restart = RestartPolicy.Always }, "A", "x", []);
        var sometimes = new SearchableCommand(Command("sometimes") with { Restart = RestartPolicy.FromExitCode }, "B", "x", []);

        var hits = SearchCommandsUseCase.Search([always, sometimes], null, new CommandSearchFilter { RequiresRestart = true });

        Assert.Equal("always", Assert.Single(hits).Command.Id);
    }

    [Theory]
    [InlineData("ping google")]
    [InlineData("typing")]
    [InlineData("shipping")]
    public void NaturalLanguage_MatchesWholeWordsOnly(string query)
        => Assert.Null(NaturalLanguageSearch.Suggest(query)); // "pin" (battery) hides inside each of these

    [Theory]
    [InlineData("pin laptop yếu", "battery")]
    [InlineData("MẠNG", "network")]
    [InlineData("Cap nhat", "update")]
    public void NaturalLanguage_IgnoresAccentsAndCase(string query, string expected)
        => Assert.Equal(expected, NaturalLanguageSearch.Suggest(query));

    private static CommandDefinition Command(string id)
        => new() { Id = id, CategoryId = "cat", TitleKey = "t", DescriptionKey = "d", Executable = "x" };

    private static SearchableCommand Make(string id, string title, string description, IReadOnlyList<string>? tags = null)
        => new(Command(id), title, description, tags ?? []);
}
