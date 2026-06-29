# Changelog

All notable changes to **CommandForge** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.4.0] — 2026-06-29

### Added
- **60 new commands and a new "Services & Startup" category** (catalog grew from 141 to 201):
  - **Services & Startup** (new 12th category): restart the Print Spooler, Windows Audio, Windows
    Update and BITS services; list running and automatic services; and open the Startup apps settings
    and the Startup folder.
  - **System info & storage:** battery health, network-adapter speed, disk health and SMART status,
    page-file usage, local user accounts, time zone and Windows install date; plus a drive-usage
    summary, largest temp files, partition list, fragmentation analysis, Disk Cleanup configuration
    and a DiskPart disk/volume listing.
  - **Network & diagnostics:** a public DNS lookup test, active TCP connections, firewall profiles,
    shared folders, the hosts file and Network Connections; plus recent boot/shutdown and
    unexpected-shutdown events, recent system errors, Print Management and Certificate Manager.
  - **Cleanup, power & maintenance:** clear the icon cache, recent items and event logs; the Power
    Saver plan and hibernate-now; and an online disk scan and a read-only system-file verification.
  - **Security, updates & tweaks:** Defender threat history and exclusions, BitLocker volume details,
    reset firewall to defaults, remove dynamic signatures and open Windows Security; update history,
    Windows Update settings, App Installer self-update, package export, third-party drivers and
    troubleshooters; and hide/show the Windows 11 taskbar search plus transparency, Snap-assist and
    Telnet-client toggles.

## [1.3.0] — 2026-06-26

### Added
- **25 new commands and a new "Storage" category** (catalog grew from 116 to 141): optimize /
  defragment C:, Disk Management, list volumes & free space, and open Storage settings; TPM status,
  Secure Boot state and a computer & OS summary; network adapters, DNS servers and an internet
  connectivity test; startup programs, Driver Verifier, Performance Monitor and a system health
  report; clear crash dumps, Windows Error Reporting data and the DNS + ARP caches; lid-close and
  monitor-timeout power toggles; and NumLock-at-startup and verbose sign-in toggles.
- **Two new UI languages — Simplified Chinese (简体中文) and Spanish (Español)** — joining English,
  Vietnamese and Japanese. Selectable in Settings and the View menu and switching live without a
  restart.

### Changed
- **Fixed main window size.** The main window is now a fixed 1100×720 and can no longer be resized
  or maximized; the F11 full-screen shortcut has been removed.

## [1.2.0] — 2026-06-21

### Added
- **38 new commands** across all ten categories (catalog grew from 78 to 116): installed
  updates / drivers / BIOS / programs / displays; active connections, ARP cache, routing table,
  Wi-Fi profiles and WinHTTP proxy; dark/light mode and Windows 11 taskbar-alignment and
  clock-seconds tweaks; thumbnail / font / Prefetch cleanup; the Balanced power plan, power
  requests and a Fast Startup toggle; Task Manager, Device Manager, Services and Group Policy
  result; winget source list and Microsoft Store reset; Defender status and full scan; firewall
  on/off; WSL shutdown, Windows Sandbox off and a long-paths toggle.
- **Japanese (日本語) language.** A full Japanese translation joins English and Vietnamese,
  selectable in Settings and the View menu and switching live without a restart.

### Changed
- **Version-aware command list.** Commands that only apply to a specific Windows version (such as
  the Windows 11 taskbar and clock tweaks) are now hidden automatically when they do not apply to
  your build of Windows.

## [1.1.1] — 2026-06-19

### Fixed
- **User commands hanging on `cmd`.** A user-defined command using `cmd` (e.g.
  `ping google.com`) could stay "running" forever without responding. The process runner now
  redirects and immediately closes the child's standard input, so an interactive `cmd` receives
  EOF and exits instead of blocking. User commands that use `cmd` also get `/c` prepended
  automatically (`cmd` + `ping google.com` runs as `cmd /c ping google.com`), and the run
  confirmation shows the exact command line. PowerShell and other executables are unchanged.
- **Recipes — duplicate steps.** A command already in a recipe can no longer be added again, so a
  recipe never runs the same command multiple times.
- **Sidebar.** Hid the vertical scrollbar on the command-groups list (still scrollable by wheel).

## [1.1.0] — 2026-06-19

### Added
- **Package & assembly metadata.** Product, author, company, copyright and repository information
  are embedded in the assemblies (shown in About and file properties) and in the Velopack
  installer.
- **Execution history.** A new History screen lists finished runs (newest first) with time, status
  and duration, plus **Run again** and **Clear history**. Stored locally; no telemetry.
- **Import / export & profiles.** Export your appearance, behavior and favorites to a `.json` file
  and import them on another machine. Save the current configuration as a named **profile**
  (e.g. "Gaming PC", "Office") and switch between them.
- **Recipes (command chains).** Build a named sequence of vetted catalog commands and run them with
  a single aggregated confirmation, **one UAC prompt** for an all-admin chain, and **stop-on-error**
  (and stop-on-reboot-required) for safety.
- **User-defined commands ("My commands").** Add your own commands. They are kept entirely separate
  from the vetted catalog and **always run without administrator rights**.
- **Registry before/after & undo.** For applicable tweaks, the affected registry values are read
  before and after the run and a diff is shown in the console (read-only). History entries for
  reversible commands offer **Undo**, which runs the command's vetted revert command — no arbitrary
  registry writes.
- **Home maintenance suggestions** (e.g. low disk space → Disk Cleanup; long uptime → restart) and
  **local natural-language search** (everyday phrases such as "clean disk" suggest a catalog
  keyword). Both are 100% local — no cloud, no LLM.

## [1.0.0] — 2026-06-18

Initial public release.

### Added
- **Command catalog.** An embedded, vetted, read-only catalog of Windows commands organized by
  category, with fuzzy search and a Command Palette (Ctrl+K) and an exact command-line preview.
- **Execution engine.** Real-time console output streamed off the UI thread, cancellation, and
  restart-required detection (exit code `3010`).
- **Admin / UAC.** Elevated commands run through a separate `CommandForge.Elevator` broker over a
  named pipe (one UAC prompt per session); the main UI stays non-elevated.
- **Safety.** Confirmation dialogs for Caution/Dangerous commands (type-to-confirm for Dangerous,
  which cannot be disabled), optional System Restore Point, and revert for reversible tweaks.
- **Self-update** via Velopack (per-user, delta updates) from GitHub Releases.
- **Localization & theming.** Full English/Vietnamese UI, light/dark/system themes, and font
  scaling.
- **Home dashboard** with recent commands, favorites and a read-only system-status widget.
- **Diagnostics.** Settings screen, Log Viewer (filter/export), Debug panel, a global crash handler
  and a "Report a bug" flow.
- **Accessibility** (automation names, keyboard navigation, AA contrast) and a first-run Legal Gate
  (EULA / GPLv3 / Disclaimer / Privacy).
- **CI/CD.** Build, test and CodeQL on every change; tag-triggered releases publish SHA-256
  checksums (and an optional VirusTotal scan). No telemetry — the only network use is checking
  GitHub for updates.

[Unreleased]: https://github.com/poli0981/CommandForge/compare/v1.3.0...HEAD
[1.3.0]: https://github.com/poli0981/CommandForge/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/poli0981/CommandForge/compare/v1.1.1...v1.2.0
[1.1.1]: https://github.com/poli0981/CommandForge/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/poli0981/CommandForge/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/poli0981/CommandForge/releases/tag/v1.0.0
