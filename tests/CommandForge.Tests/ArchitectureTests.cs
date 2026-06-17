using System.Reflection;
using CommandForge.Application;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>
/// Enforces the Clean Architecture boundary (golden rule #4): the Domain and Application
/// layers must not reference WPF or Win32 assemblies.
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
    ];

    [Fact]
    public void Domain_DoesNotReferenceUiOrWin32()
        => AssertNoForbiddenReferences(typeof(CommandDefinition).Assembly);

    [Fact]
    public void Application_DoesNotReferenceUiOrWin32()
        => AssertNoForbiddenReferences(typeof(LegalGateService).Assembly);

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
