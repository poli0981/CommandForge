using System.Reflection;
using CommandForge.Application;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>
/// Enforces the Clean Architecture boundary (golden rule #4): the Domain and Application
/// layers must not reference WPF, Win32 or any infrastructure library.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "PresentationFramework",
        "PresentationCore",
        "WindowsBase",
        "System.Windows",
        "Microsoft.Windows",
        "Microsoft.Win32",
        "System.Management",
        "Microsoft.Extensions",
        "Serilog",
        "Velopack",
    ];

    [Fact]
    public void Domain_DoesNotReferenceUiOrWin32()
        => AssertNoForbiddenReferences(typeof(CommandDefinition).Assembly);

    [Fact]
    public void Application_DoesNotReferenceUiOrWin32()
        => AssertNoForbiddenReferences(typeof(LegalGateService).Assembly);

    [Fact]
    public void Domain_ReferencesOnlyTheRuntime()
        => Assert.All(
            typeof(CommandDefinition).Assembly.GetReferencedAssemblies(),
            reference => Assert.True(IsRuntime(reference), $"Domain must not reference {reference.Name}."));

    [Fact]
    public void Application_ReferencesOnlyTheRuntimeAndDomain()
        => Assert.All(
            typeof(LegalGateService).Assembly.GetReferencedAssemblies(),
            reference => Assert.True(
                IsRuntime(reference) || reference.Name == typeof(CommandDefinition).Assembly.GetName().Name,
                $"Application must not reference {reference.Name}."));

    private static bool IsRuntime(AssemblyName reference)
        => reference.Name is { } name
            && (name.StartsWith("System.", StringComparison.Ordinal) || name is "System" or "mscorlib" or "netstandard");

    private static void AssertNoForbiddenReferences(Assembly assembly)
    {
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            var name = reference.Name ?? string.Empty;
            foreach (var forbidden in ForbiddenAssemblyPrefixes)
            {
                Assert.False(
                    name.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"{assembly.GetName().Name} must not reference {name}.");
            }
        }
    }
}
