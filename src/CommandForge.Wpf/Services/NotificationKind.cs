namespace CommandForge.Desktop.Services;

/// <summary>The tone of a notification (drives its icon and color).</summary>
public enum NotificationKind
{
    /// <summary>Neutral information.</summary>
    Information,

    /// <summary>Something finished well.</summary>
    Success,

    /// <summary>Needs attention but nothing failed.</summary>
    Warning,

    /// <summary>Something failed.</summary>
    Error,
}
