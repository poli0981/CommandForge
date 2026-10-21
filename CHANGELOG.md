# Changelog

All notable changes to **CommandForge** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Settings → Advanced and Portable info now say whether this copy updates itself ("Installed or
  portable" / "Development build") instead of "Installed" / "Portable / dev": the portable zip
  from the releases page updates itself like the installed app.
- Releases: the notes come from this changelog and list the VirusTotal reports; the provenance
  attestations now also cover the portable zip, and `checksums.txt` lists exactly the files you
  can download.

## [2.0.0] — 2026-10-21

A redesign in the Fluent style of Windows 11, plus a broad safety, reliability and security
overhaul. Settings, favorites, history, recipes and your own commands carry over unchanged.

### Added
- **Fluent UI (WPF-UI 4.3).** A Mica window with its own title bar, a navigation pane (Home,
  Favorites, History, Recipes, My commands and the catalog with its 12 categories and counts; Log
  viewer, Debug panel and Settings at the bottom), in-window dialogs, snackbar notifications and
  the Windows accent color. Light, dark or system theme; High Contrast always wins.
- **Resizable window** (minimum 960×600, default 1200×780) that remembers its size, position and
  maximized state, and never reopens off-screen.
- **Title bar:** a search box for the whole catalog (`Ctrl+K`), an admin-session indicator with
  **End admin session**, and a **More** menu (Command Palette, Check for updates, Create System
  Restore Point, export logs, open the config and log folders, keyboard shortcuts, user guide,
  report a bug, terms, About).
- **Catalog:** filter chips for Safe / Caution / Dangerous and Admin only, the estimated duration,
  the command line with paths filled in, and a splitter between the details and the console.
- **Console:** one shared console for the catalog, recipes and your own commands — virtualized,
  capped at 10,000 lines (with an "N earlier lines" note), **Copy all output**, **Save output to a
  file**, a "Waiting for administrator permission…" status, and **Restart now** (asks first,
  never while a command runs).
- A finished run shows a notification and flashes the taskbar button when the console isn't
  visible; the taskbar button shows progress while a command runs.
- The Legal Gate lets you choose the app language before reading the terms.
- **20 new commands** (catalog grew from 241 to 261): restore point list, System Restore, Automatic
  Maintenance, clock resync, DNS cache, listening ports, Wi-Fi interface details, third-party
  scheduled tasks, Run keys, stopped automatic services, Windows Search restart, Store and optional
  updates, UAC status, Core isolation, environment variables, WSL status, TRIM status, crash dumps
  and battery charge.
- A **user guide** ([USAGE.md](USAGE.md)), linked from the More menu and About. About also lists
  contact addresses for security reports and code questions.
- **Release integrity:** build-provenance attestations (`gh attestation verify`), an SPDX SBOM and
  SHA-256 checksums are attached to every release.

### Changed
- **One set of safety rules.** Dangerous commands always ask with typing; Caution commands ask
  unless you turn that off; a restore point is offered only for risky admin commands (or commands
  flagged for one), so per-user tweaks no longer trigger a UAC prompt for it.
- **Type-to-confirm** accepts the command name regardless of case and Unicode form, or the command
  ID, so Vietnamese input methods and Japanese/Chinese IMEs can confirm. Cancel is the default.
- **Recipes** no longer run Dangerous steps after a single Yes/No: an overview prompt is followed
  by each risky step's own confirmation, with one restore point before the chain.
- **Search** returns relevant matches only (descriptions and tags must contain your words), ranks
  titles better, highlights the right letters and ignores accents ("don dep" finds "Dọn dẹp").
- **Catalog safety and accuracy:**
  - Automatic Disk Cleanup uses its own allow-list of safe handlers, so Downloads, the Recycle
    Bin and Windows.old are never touched and your own `/sageset:1` choices are kept.
  - Removing Windows.old and clearing the event logs are now Dangerous.
  - Drive repair uses `Repair-Volume -OfflineScanAndFix`; drive letters and profile paths come
    from your actual system drive and profile.
  - PowerShell commands run with `-NoProfile -NonInteractive` and stop at the first error instead
    of always printing success; cleanups report what they removed and what was in use.
  - winget never prompts; activation status no longer needs VBScript; the Windows 11 Widgets
    button opens Settings, because Windows now blocks that change from scripts.
- Switching the language changes only the app's text; number and date formats keep following
  your regional settings.
- **Logs:** 14 daily files of at most 10 MB each (they grew without limit before). Every run is
  logged as a one-line summary (ID, exit code, duration), never with its output.
- Faster startup: ReadyToRun publish and no Generic Host (about 20 fewer packages).
- Font size now scales every text and control live.

### Removed
- MaterialDesignThemes, Microsoft.Extensions.Hosting and Serilog.Extensions.Hosting.

### Fixed
- **Garbled output** from `sfc`, `wsl` and other native tools on non-English Windows: output is
  decoded as UTF-16, UTF-8 or the OEM code page, whichever it is.
- The console could stay "running" forever when a run failed or was cancelled while the admin
  helper was starting.
- A failed startup left an invisible process holding the single-instance lock, so every later
  launch silently did nothing. The app now shows an error and exits.
- Settings, history, recipes, profiles and your commands are saved atomically. A damaged file is
  kept as a backup instead of silently resetting everything (including the accepted terms).
- **Updates:** closing the update dialog with X cancels the download; the app no longer restarts
  mid-command to apply an update; timeouts and other errors are reported instead of leaving the
  dialog on "Checking".
- **Restore points:** "Windows already made one in the last 24 hours" is reported as skipped, not
  as created; any other failure asks before running the command anyway.
- **Keyboard:** AltGr no longer triggers shortcuts (typing "@" on a German keyboard closed the
  app); `Ctrl+B` toggles the navigation pane as advertised.
- History's Run again and Undo are disabled while a command runs; runs that never started are not
  recorded.
- My commands: a quoted `cmd` is recognized and `/c` is no longer added twice.
- Exporting logs or settings to a locked path, or opening a link with no browser set up, shows a
  message instead of the crash dialog. Imported settings files are validated.
- Crash support codes stay the same across runs.
- The Log Viewer refreshes once per burst of log events instead of once per event.

### Security
- Catalog programs resolve to absolute paths in System32/Windows, so a program planted in the
  app's per-user folder can't run elevated; Launch commands pin their working directory.
- Elevated commands get an environment rebuilt from trusted sources: per-user variables such as
  `PSModulePath`, `COR_PROFILER`, `SystemRoot` and `windir` can no longer redirect what an elevated
  command loads or deletes. The elevated helper ignores `DOTNET_STARTUP_HOOKS`.
- The app and the elevated helper verify each other's process on the named pipe; the helper
  connects with Identification-level impersonation and no longer writes files.
- Captured commands run in a kill-on-close job object.
- The catalog loader registers a command only after it passes validation; contradictory or unsafe
  records are rejected.
- **CI and supply chain:** actions pinned to commit SHAs with least-privilege permissions, CodeQL
  for C# and GitHub Actions, dependency review, OpenSSF Scorecard, a NuGet audit of transitive
  packages, SonarAnalyzer rules enforced at build time, and a protected `main` branch with
  required checks.

## [1.5.1] — 2026-07-09

### Fixed
- **Single instance:** launching the app while it is already running no longer opens a second
  window. Later launches bring the existing window to the foreground (restoring it if minimized)
  and then exit.

## [1.5.0] — 2026-07-08

### Added
- **40 new commands** (catalog grew from 201 to 241), focused on rarely-used and Windows-version-specific
  features, gated by OS build so Windows 10-only commands are hidden on Windows 11 and vice-versa:
  - **Windows 11-only tweaks & pages:** hide the Widgets and Copilot taskbar buttons; enable taskbar
    "End task"; "Never combine + labels" taskbar; "More pins" Start layout; and open Dynamic Lighting,
    Presence sensing and (24H2) Energy Saver settings.
  - **Windows 10-only tweaks:** turn off News and Interests; hide the Meet Now, People and Cortana
    buttons; and turn off all Start menu live tiles.
  - **Diagnostics & system info:** current-user rights & groups, Start menu app IDs, Boot Configuration
    (BCD), devices reporting problems, all event-log channels and Entra/AD device-join state.
  - **Security, storage, power, network & developer:** local Administrators members, trusted root
    certificates, all firewall rules and Recall feature status (24H2); SMART reliability counters,
    volume dirty bit and sector-size info; active wake timers and reduced-size hibernation; Wi-Fi radio
    capabilities, global TCP settings and a Wi-Fi history report; and Dev Drive status, Sudo for Windows
    status, the Hyper-V VM list and the boot hypervisor launch type.

### Fixed
- **TPM Status** and **Defender Exclusions** now run with administrator elevation, so they return
  results instead of appearing blank (both underlying queries require an elevated session).

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

[Unreleased]: https://github.com/poli0981/CommandForge/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/poli0981/CommandForge/compare/v1.5.1...v2.0.0
[1.5.1]: https://github.com/poli0981/CommandForge/compare/v1.5.0...v1.5.1
[1.5.0]: https://github.com/poli0981/CommandForge/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/poli0981/CommandForge/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/poli0981/CommandForge/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/poli0981/CommandForge/compare/v1.1.1...v1.2.0
[1.1.1]: https://github.com/poli0981/CommandForge/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/poli0981/CommandForge/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/poli0981/CommandForge/releases/tag/v1.0.0
