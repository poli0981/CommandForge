using CommandForge.Application.Ports;
using CommandForge.Domain;

namespace CommandForge.Application.Registry;

/// <summary>A single before→after change of a registry value.</summary>
public sealed record RegistryChange(RegistryValueRef Reference, string? Before, string? After);

/// <summary>Pure helpers to snapshot and diff a set of registry values (read-only).</summary>
public static class RegistrySnapshot
{
    /// <summary>Reads the current value of each reference into a snapshot.</summary>
    public static IReadOnlyDictionary<RegistryValueRef, string?> Capture(
        IRegistryService registry,
        IReadOnlyList<RegistryValueRef> references)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(references);

        var snapshot = new Dictionary<RegistryValueRef, string?>();
        foreach (var reference in references)
        {
            snapshot[reference] = registry.Read(reference);
        }

        return snapshot;
    }

    /// <summary>Returns the references whose value changed between the two snapshots.</summary>
    public static IReadOnlyList<RegistryChange> Diff(
        IReadOnlyList<RegistryValueRef> references,
        IReadOnlyDictionary<RegistryValueRef, string?> before,
        IReadOnlyDictionary<RegistryValueRef, string?> after)
    {
        ArgumentNullException.ThrowIfNull(references);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var changes = new List<RegistryChange>();
        foreach (var reference in references)
        {
            before.TryGetValue(reference, out var b);
            after.TryGetValue(reference, out var a);
            if (!string.Equals(b, a, StringComparison.Ordinal))
            {
                changes.Add(new RegistryChange(reference, b, a));
            }
        }

        return changes;
    }
}
