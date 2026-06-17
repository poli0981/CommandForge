namespace CommandForge.Domain;

/// <summary>A single line of process output, tagged by which stream it came from.</summary>
/// <param name="Text">The line text.</param>
/// <param name="IsError">Whether the line came from stderr.</param>
public readonly record struct OutputLine(string Text, bool IsError);
