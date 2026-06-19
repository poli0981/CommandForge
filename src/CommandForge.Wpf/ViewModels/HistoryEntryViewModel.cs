using System.Globalization;
using CommandForge.Domain;
using CommandForge.Wpf.Resources;

namespace CommandForge.Wpf.ViewModels;

/// <summary>One row in the execution-history list: a past run with its resolved title and display strings.</summary>
public sealed class HistoryEntryViewModel
{
    private readonly ExecutionRecord _record;

    public HistoryEntryViewModel(ExecutionRecord record, string title, string? icon, bool canRun, string? revertCommandId)
    {
        ArgumentNullException.ThrowIfNull(record);

        _record = record;
        Title = title;
        Icon = icon;
        CanRun = canRun;
        RevertCommandId = revertCommandId;
    }

    /// <summary>The catalog id of the command that ran.</summary>
    public string CommandId => _record.CommandId;

    /// <summary>Resolved command title, or the raw id if the command is no longer in the catalog.</summary>
    public string Title { get; }

    /// <summary>Material icon name, or null if the command is no longer in the catalog.</summary>
    public string? Icon { get; }

    /// <summary>Whether the command still exists in the catalog (so it can be opened / re-run).</summary>
    public bool CanRun { get; }

    /// <summary>Id of the vetted catalog command that reverts this run, or null if not reversible.</summary>
    public string? RevertCommandId { get; }

    /// <summary>Whether this run can be undone by running its (vetted) revert command.</summary>
    public bool CanUndo => RevertCommandId is not null;

    /// <summary>The run time, formatted in the local time zone and current culture.</summary>
    public string TimeText => _record.Timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    /// <summary>Localized status — reuses the execution-result strings (Success / Failed).</summary>
    public string StatusText => Strings.Get(_record.Success ? "Result_Success" : "Result_Failed");

    /// <summary>Whether the run succeeded (drives the status colour in the view).</summary>
    public bool Success => _record.Success;

    /// <summary>Human-readable duration (milliseconds under one second, otherwise seconds).</summary>
    public string DurationText => _record.DurationMs < 1000
        ? string.Format(CultureInfo.CurrentCulture, "{0} ms", _record.DurationMs)
        : string.Format(CultureInfo.CurrentCulture, "{0:F1} s", _record.DurationMs / 1000d);
}
