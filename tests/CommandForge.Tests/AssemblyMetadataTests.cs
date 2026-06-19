using System.Reflection;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>
/// Verifies the package/assembly metadata declared in Directory.Build.props is stamped into
/// the built assemblies. This metadata is surfaced in Settings -> About, the Report-Bug
/// template, the file properties, and the Velopack installer (--packAuthors).
/// </summary>
public sealed class AssemblyMetadataTests
{
    // Domain is the lowest layer and inherits the same Directory.Build.props metadata as the rest.
    private static readonly Assembly Target = typeof(CommandDefinition).Assembly;

    [Fact]
    public void Assembly_HasExpectedCompany()
        => Assert.Equal("poli0981", Target.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company);

    [Fact]
    public void Assembly_HasExpectedProduct()
        => Assert.Equal("CommandForge", Target.GetCustomAttribute<AssemblyProductAttribute>()?.Product);

    [Fact]
    public void Assembly_HasCopyrightWithOwner()
    {
        var copyright = Target.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;

        Assert.NotNull(copyright);
        Assert.Contains("poli0981", copyright, StringComparison.Ordinal);
    }
}
