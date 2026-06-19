using CommandForge.Application.Ports;
using CommandForge.Application.Registry;
using CommandForge.Domain;
using CommandForge.Infrastructure.Catalog;

namespace CommandForge.Tests;

/// <summary>Tests the read-only registry snapshot/diff helpers and the catalog schema field.</summary>
public sealed class RegistrySnapshotTests
{
    private static RegistryValueRef Ref(string name) => new() { Path = "HKCU\\Software\\X", Name = name };

    [Fact]
    public void Diff_ReturnsOnlyChangedValues()
    {
        var a = Ref("A");
        var b = Ref("B");
        var before = new Dictionary<RegistryValueRef, string?> { [a] = "1", [b] = "x" };
        var after = new Dictionary<RegistryValueRef, string?> { [a] = "0", [b] = "x" };

        var change = Assert.Single(RegistrySnapshot.Diff([a, b], before, after));

        Assert.Equal(a, change.Reference);
        Assert.Equal("1", change.Before);
        Assert.Equal("0", change.After);
    }

    [Fact]
    public void Diff_TreatsNullToValueAsChange()
    {
        var a = Ref("A");
        var before = new Dictionary<RegistryValueRef, string?> { [a] = null };
        var after = new Dictionary<RegistryValueRef, string?> { [a] = "1" };

        var change = Assert.Single(RegistrySnapshot.Diff([a], before, after));

        Assert.Null(change.Before);
        Assert.Equal("1", change.After);
    }

    [Fact]
    public void Capture_ReadsEachReference()
    {
        var a = Ref("A");
        var registry = new FakeRegistry(new Dictionary<RegistryValueRef, string?> { [a] = "42" });

        Assert.Equal("42", RegistrySnapshot.Capture(registry, [a])[a]);
    }

    [Fact]
    public void Catalog_ShowExt_DeclaresAffectedRegistryValue()
    {
        var catalog = CatalogLoader.LoadEmbedded();
        var showExt = catalog.Commands.Single(c => c.Id == "tweak.showext");

        var value = Assert.Single(showExt.AffectedRegistryValues);
        Assert.Equal("HideFileExt", value.Name);
        Assert.Contains("Explorer", value.Path, StringComparison.Ordinal);
    }

    private sealed class FakeRegistry(Dictionary<RegistryValueRef, string?> values) : IRegistryService
    {
        public string? Read(RegistryValueRef reference) => values.GetValueOrDefault(reference);
    }
}
