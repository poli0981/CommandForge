using System.Collections.ObjectModel;
using System.Globalization;
using CommandForge.Application.Ports;
using CommandForge.Wpf.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommandForge.Wpf.ViewModels;

/// <summary>
/// Home dashboard: recently-run commands, pinned favorites, and a read-only system-status widget.
/// The lists share the catalog's <see cref="CommandItemViewModel"/> instances (so favorite/highlight
/// state stays in sync) and are rebuilt — along with a fresh status read — on each navigation to Home.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private const double BytesPerGigabyte = 1024d * 1024d * 1024d;

    private readonly IReadOnlyDictionary<string, CommandItemViewModel> _itemsById;
    private readonly ISettingsService _settings;
    private readonly ISystemInfoService _systemInfo;
    private readonly Action<CommandItemViewModel> _openCommand;

    public HomeViewModel(
        IReadOnlyDictionary<string, CommandItemViewModel> itemsById,
        ISettingsService settings,
        ISystemInfoService systemInfo,
        Action<CommandItemViewModel> openCommand)
    {
        ArgumentNullException.ThrowIfNull(itemsById);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(systemInfo);
        ArgumentNullException.ThrowIfNull(openCommand);

        _itemsById = itemsById;
        _settings = settings;
        _systemInfo = systemInfo;
        _openCommand = openCommand;
    }

    /// <summary>Recently-run commands, most-recent first.</summary>
    public ObservableCollection<CommandItemViewModel> Recent { get; } = [];

    /// <summary>Pinned favorite commands, in pin order.</summary>
    public ObservableCollection<CommandItemViewModel> Favorites { get; } = [];

    [ObservableProperty]
    private bool _hasRecent;

    [ObservableProperty]
    private bool _hasFavorites;

    [ObservableProperty]
    private string _osValue = string.Empty;

    [ObservableProperty]
    private string _memoryValue = string.Empty;

    [ObservableProperty]
    private string _diskValue = string.Empty;

    [ObservableProperty]
    private string _uptimeValue = string.Empty;

    /// <summary>Rebuilds the recent/favorites lists from settings and re-reads system status.</summary>
    public void Refresh()
    {
        Rebuild(Recent, _settings.RecentCommandIds);
        HasRecent = Recent.Count > 0;

        Rebuild(Favorites, _settings.FavoriteCommandIds);
        HasFavorites = Favorites.Count > 0;

        RefreshStatus();
    }

    private void Rebuild(ObservableCollection<CommandItemViewModel> target, IReadOnlyList<string> ids)
    {
        target.Clear();
        foreach (var id in ids)
        {
            // Ignore ids no longer present in the catalog (e.g. a command removed between versions).
            if (_itemsById.TryGetValue(id, out var item))
            {
                target.Add(item);
            }
        }
    }

    private void RefreshStatus()
    {
        var status = _systemInfo.GetStatus();
        var culture = CultureInfo.CurrentCulture;

        OsValue = string.Format(culture, Strings.Get("Home_OsFormat"), status.OsName, status.OsBuild);
        MemoryValue = string.Format(
            culture,
            Strings.Get("Home_StorageFormat"),
            FormatGigabytes(status.AvailableMemoryBytes),
            FormatGigabytes(status.TotalMemoryBytes));
        var diskStorage = string.Format(
            culture,
            Strings.Get("Home_StorageFormat"),
            FormatGigabytes((ulong)Math.Max(0L, status.SystemDriveFreeBytes)),
            FormatGigabytes((ulong)Math.Max(0L, status.SystemDriveTotalBytes)));
        DiskValue = string.Format(culture, Strings.Get("Home_DiskFormat"), status.SystemDriveName, diskStorage);
        UptimeValue = FormatUptime(status.Uptime);
    }

    [RelayCommand]
    private void Open(CommandItemViewModel? item)
    {
        if (item is not null)
        {
            _openCommand(item);
        }
    }

    private static string FormatGigabytes(ulong bytes)
        => string.Format(CultureInfo.CurrentCulture, Strings.Get("Home_GigabytesFormat"), bytes / BytesPerGigabyte);

    private static string FormatUptime(TimeSpan uptime)
    {
        var days = (int)uptime.TotalDays;
        var key = days > 0 ? "Home_UptimeWithDays" : "Home_UptimeNoDays";
        return string.Format(CultureInfo.CurrentCulture, Strings.Get(key), days, uptime.Hours, uptime.Minutes);
    }
}
