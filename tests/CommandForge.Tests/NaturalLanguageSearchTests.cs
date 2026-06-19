using CommandForge.Application.Search;

namespace CommandForge.Tests;

/// <summary>Tests the local natural-language → keyword mapping (no LLM/network).</summary>
public sealed class NaturalLanguageSearchTests
{
    [Theory]
    [InlineData("dọn ổ đĩa", "cleanup")]
    [InlineData("free space please", "cleanup")]
    [InlineData("reset my network", "network")]
    [InlineData("kiểm tra cập nhật", "update")]
    [InlineData("battery report", "battery")]
    public void Suggest_MapsNaturalPhrase_ToKeyword(string query, string expected)
        => Assert.Equal(expected, NaturalLanguageSearch.Suggest(query));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("systeminfo")]
    [InlineData("cleanup")] // already the keyword — don't suggest what was typed
    public void Suggest_ReturnsNull_WhenNoMappingOrAlreadyKeyword(string query)
        => Assert.Null(NaturalLanguageSearch.Suggest(query));
}
