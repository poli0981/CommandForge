namespace CommandForge.Tests;

/// <summary>
/// Tests that switch the process-wide UI language. They run on their own, after the parallel tests,
/// so a test elsewhere never reads a string while the language is changing under it.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CultureSensitiveCollection
{
    public const string Name = "Culture-sensitive";

    private CultureSensitiveCollection()
    {
    }
}
