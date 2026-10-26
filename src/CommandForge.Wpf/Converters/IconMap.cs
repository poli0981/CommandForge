using Wpf.Ui.Controls;

namespace CommandForge.Desktop.Converters;

/// <summary>
/// Maps the catalog's icon names (Material Design icon names, kept so the catalog schema stays
/// stable) to Fluent System Icons. A test checks that every icon the catalog uses is mapped.
/// </summary>
public static class IconMap
{
    /// <summary>The icon shown for a name that is missing or unknown.</summary>
    public const SymbolRegular Fallback = SymbolRegular.ChevronRight24;

    private static readonly Dictionary<string, SymbolRegular> Symbols = new(StringComparer.OrdinalIgnoreCase)
    {
        // Commands
        ["Account"] = SymbolRegular.Person24,
        ["AccountGroup"] = SymbolRegular.PeopleCommunity24,
        ["AccountKey"] = SymbolRegular.PersonKey24,
        ["Alert"] = SymbolRegular.Warning24,
        ["Battery"] = SymbolRegular.Battery1024,
        ["Bluetooth"] = SymbolRegular.Bluetooth24,
        ["Brightness4"] = SymbolRegular.DarkTheme24,
        ["Brightness7"] = SymbolRegular.WeatherSunny24,
        ["Broom"] = SymbolRegular.Broom24,
        ["Certificate"] = SymbolRegular.Certificate24,
        ["ChartBar"] = SymbolRegular.DataBarVertical24,
        ["ChartBox"] = SymbolRegular.ChartMultiple24,
        ["ChartLine"] = SymbolRegular.DataLine24,
        ["Chip"] = SymbolRegular.DeveloperBoard24,
        ["ClipboardListOutline"] = SymbolRegular.ClipboardTaskListLtr24,
        ["ClipboardPulse"] = SymbolRegular.ClipboardPulse24,
        ["ClockOutline"] = SymbolRegular.Clock24,
        ["CloudUpload"] = SymbolRegular.CloudArrowUp24,
        ["Cog"] = SymbolRegular.Settings24,
        ["CogOutline"] = SymbolRegular.Settings24,
        ["Console"] = SymbolRegular.WindowConsole20,
        ["Database"] = SymbolRegular.Database24,
        ["DeleteSweep"] = SymbolRegular.DeleteDismiss24,
        ["Dns"] = SymbolRegular.Server24,
        ["Ethernet"] = SymbolRegular.Router24,
        ["Eye"] = SymbolRegular.Eye24,
        ["EyeOff"] = SymbolRegular.EyeOff24,
        ["FileAlertOutline"] = SymbolRegular.DocumentError24,
        ["FileCheck"] = SymbolRegular.DocumentCheckmark24,
        ["FileCog"] = SymbolRegular.DocumentSettings20,
        ["FolderOpen"] = SymbolRegular.FolderOpen24,
        ["FormatListChecks"] = SymbolRegular.TaskListLtr24,
        ["Gamepad"] = SymbolRegular.Games24,
        ["Harddisk"] = SymbolRegular.HardDrive20,
        ["History"] = SymbolRegular.History24,
        ["Information"] = SymbolRegular.Info24,
        ["Ip"] = SymbolRegular.Globe24,
        ["KeyChain"] = SymbolRegular.KeyMultiple24,
        ["KeyVariant"] = SymbolRegular.Key24,
        ["Keyboard"] = SymbolRegular.Keyboard24,
        ["Lan"] = SymbolRegular.NetworkCheck24,
        ["LanConnect"] = SymbolRegular.Link24,
        ["Laptop"] = SymbolRegular.Laptop24,
        ["Lock"] = SymbolRegular.LockClosed24,
        ["Magnify"] = SymbolRegular.Search24,
        ["Memory"] = SymbolRegular.DataUsage24,
        ["MessageTextOutline"] = SymbolRegular.Chat24,
        ["Microsoft"] = SymbolRegular.WindowApps24,
        ["Monitor"] = SymbolRegular.Desktop24,
        ["MonitorOff"] = SymbolRegular.DesktopOff24,
        ["PackageVariant"] = SymbolRegular.Box24,
        ["Power"] = SymbolRegular.Power24,
        ["PowerPlug"] = SymbolRegular.BatteryCharge24,
        ["PowerSleep"] = SymbolRegular.WeatherMoon24,
        ["Printer"] = SymbolRegular.Print24,
        ["Restore"] = SymbolRegular.ArrowCounterclockwise24,
        ["Rocket"] = SymbolRegular.Rocket24,
        ["ShieldCheck"] = SymbolRegular.ShieldCheckmark24,
        ["ShieldOff"] = SymbolRegular.ShieldDismiss24,
        ["ShieldRefresh"] = SymbolRegular.ShieldTask24,
        ["ShieldSearch"] = SymbolRegular.ShieldQuestion24,
        ["Sync"] = SymbolRegular.ArrowSync24,
        ["Translate"] = SymbolRegular.LocalLanguage24,
        ["Update"] = SymbolRegular.ArrowSyncCircle24,
        ["Usb"] = SymbolRegular.UsbStick24,
        ["ViewGridPlus"] = SymbolRegular.AppsAddIn24,
        ["VolumeHigh"] = SymbolRegular.Speaker224,
        ["Webcam"] = SymbolRegular.Camera24,
        ["Wifi"] = SymbolRegular.Wifi124,

        // Categories
        ["AccountMultiple"] = SymbolRegular.People24,
        ["BackupRestore"] = SymbolRegular.ArchiveArrowBack24,
        ["CodeBraces"] = SymbolRegular.Code24,
        ["Devices"] = SymbolRegular.PhoneLaptop24,
        ["ShieldLock"] = SymbolRegular.ShieldLock24,
        ["Stethoscope"] = SymbolRegular.Stethoscope24,
        ["Tune"] = SymbolRegular.Options24,
        ["Wrench"] = SymbolRegular.Wrench24,

        // The "All commands" pseudo-category
        ["ViewGridOutline"] = SymbolRegular.Grid24,
    };

    /// <summary>Whether <paramref name="name"/> has a mapping of its own (not the fallback).</summary>
    public static bool IsMapped(string? name) => name is not null && Symbols.ContainsKey(name);

    /// <summary>The Fluent symbol for a catalog icon name, or <see cref="Fallback"/>.</summary>
    public static SymbolRegular Resolve(string? name)
        => name is not null && Symbols.TryGetValue(name, out var symbol) ? symbol : Fallback;
}
