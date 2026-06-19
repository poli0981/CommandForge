# Changelog

All notable changes to **CommandForge** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

_Nothing yet._

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

[Unreleased]: https://github.com/poli0981/CommandForge/compare/v1.1.1...HEAD
[1.1.1]: https://github.com/poli0981/CommandForge/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/poli0981/CommandForge/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/poli0981/CommandForge/releases/tag/v1.0.0
