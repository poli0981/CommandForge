using CommandForge.Domain;
using CommandForge.Infrastructure.Catalog;

namespace CommandForge.Tests;

/// <summary>Verifies the OS-build applicability predicate and the embedded catalog's version gating.</summary>
public sealed class CommandDefinitionTests
{
    private static CommandDefinition Command(int? min = null, int? max = null) => new()
    {
        Id = "t",
        CategoryId = "c",
        TitleKey = "T",
        DescriptionKey = "D",
        Executable = "cmd",
        MinOsBuild = min,
        MaxOsBuild = max,
    };

    [Theory]
    [InlineData(19045, true)]   // no bounds -> always applies
    [InlineData(22631, true)]
    public void AppliesToOsBuild_NoBounds_AlwaysTrue(int build, bool expected)
        => Assert.Equal(expected, Command().AppliesToOsBuild(build));

    [Theory]
    [InlineData(19045, false)]  // Windows 10 excluded by min 22000
    [InlineData(21999, false)]
    [InlineData(22000, true)]   // inclusive lower bound
    [InlineData(26100, true)]
    public void AppliesToOsBuild_MinOnly_GatesBelow(int build, bool expected)
        => Assert.Equal(expected, Command(min: 22000).AppliesToOsBuild(build));

    [Theory]
    [InlineData(19045, true)]   // inclusive upper bound (Windows 10 only)
    [InlineData(22000, false)]  // Windows 11 excluded by max 19045
    public void AppliesToOsBuild_MaxOnly_GatesAbove(int build, bool expected)
        => Assert.Equal(expected, Command(max: 19045).AppliesToOsBuild(build));

    [Theory]
    [InlineData(21999, false)]
    [InlineData(22000, true)]
    [InlineData(22621, true)]
    [InlineData(22622, false)]
    public void AppliesToOsBuild_Range_GatesBothEnds(int build, bool expected)
        => Assert.Equal(expected, Command(min: 22000, max: 22621).AppliesToOsBuild(build));

    [Theory]
    [InlineData("tweak.win11.classicmenu", 22000)]
    [InlineData("tweak.win11.modernmenu", 22000)]
    [InlineData("tweak.win11.taskbarleft", 22000)]
    [InlineData("tweak.win11.taskbarcenter", 22000)]
    [InlineData("tweak.clockseconds.on", 22621)]
    [InlineData("tweak.clockseconds.off", 22621)]
    public void EmbeddedCatalog_Win11Commands_AreGated(string id, int expectedMinBuild)
    {
        var catalog = CatalogLoader.LoadEmbedded();
        var command = catalog.Commands.Single(c => c.Id == id);
        Assert.Equal(expectedMinBuild, command.MinOsBuild);
    }

    [Fact]
    public void EmbeddedCatalog_Win11Tweaks_HiddenOnWindows10()
    {
        const int windows10Build = 19045;
        var catalog = CatalogLoader.LoadEmbedded();
        var hiddenOnWin10 = catalog.Commands.Where(c => !c.AppliesToOsBuild(windows10Build)).Select(c => c.Id).ToHashSet();

        Assert.Contains("tweak.win11.taskbarleft", hiddenOnWin10);
        Assert.Contains("tweak.clockseconds.on", hiddenOnWin10);
        Assert.DoesNotContain("dism.checkhealth", hiddenOnWin10); // ungated commands stay visible
    }
}
