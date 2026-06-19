# CommandForge

> Run powerful Windows maintenance commands from a clean, categorized GUI — no need to
> memorize syntax.

CommandForge is an open-source Windows desktop app that wraps common system commands
(winget, DISM, SFC, God Mode, registry tweaks, network reset, power plans, diagnostics…)
in a visual, categorized interface. Every command is shown to you **before** it runs, with a
clear danger level, optional confirmation, and an optional System Restore Point.

- **Windows 10 (22H2) and Windows 11**, x64.
- **Open source, GPLv3.**
- **Offline-first. No telemetry.** The only network call is to GitHub to check for updates.
- Installed and auto-updated with [Velopack](https://velopack.io/) (per-user, no UAC for updates).

> **Status:** pre‑1.0. Built by a solo developer with AI assistance (see
> [Disclaimer](DISCLAIMER.md) and the AI disclosure below).

---

## Features

- **Categorized command catalog** — browse by group (System & Maintenance, Updates & Packages,
  Tweaks & Shortcuts, Network, Cleanup, Power, System Info, Diagnostics, Security & Privacy,
  Developer) with fuzzy search and a Command Palette (`Ctrl+K`).
- **Safe by design** — the command catalog is a vetted, read-only embedded resource. The app
  never loads arbitrary commands and self-elevates. Admin commands run through a separate
  elevation broker (one UAC prompt per session).
- **See before you run** — the exact command line is previewed; `Caution`/`Dangerous` commands
  require confirmation (type-to-confirm for `Dangerous`), with an optional Restore Point.
- **Real-time console** — stream output, cancel long-running commands, detect reboot-required
  (`exit 3010`).
- **Home dashboard** — recent commands, pinned favorites, and a read-only system-status widget
  (Windows build, RAM, system drive, uptime).
- **Polish** — light/dark/system theme, font scaling, live English/Vietnamese switching, a Log
  Viewer, a Debug panel, and a global crash handler with a "Report a bug" flow.

---

## Install

1. Download the latest **`CommandForge-win-Setup.exe`** from the
   [Releases page](https://github.com/poli0981/CommandForge/releases).
2. Run it. The app installs per-user and updates itself from future releases.

### "Unknown publisher" / SmartScreen

CommandForge is **not code-signed yet** (see [SECURITY.md](.github/SECURITY.md)), so Windows
SmartScreen may warn about an unknown publisher and some antivirus engines may show a false
positive (common for system-administration tools). To proceed: **More info → Run anyway**.

To verify your download, compare its SHA-256 against the `checksums.txt` attached to the release,
and review the VirusTotal link in the release notes:

```powershell
Get-FileHash .\CommandForge-win-Setup.exe -Algorithm SHA256
```

---

## Build from source

Requires the **.NET 10 SDK** on Windows 10 22H2 / 11.

```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/CommandForge.Wpf
dotnet test
```

See [BUILD.md](BUILD.md) for publish/packaging and [UPDATES.md](UPDATES.md) for how the
Velopack update mechanism works.

---

## Documentation

| Topic | File |
|---|---|
| Release history | [CHANGELOG.md](CHANGELOG.md) |
| Build, test, publish, release | [BUILD.md](BUILD.md) · [RELEASING.md](RELEASING.md) |
| Update mechanism (Velopack) | [UPDATES.md](UPDATES.md) |
| Contributing & contribution rules | [CONTRIBUTING.md](.github/CONTRIBUTING.md) |
| Security policy & disclosure | [SECURITY.md](.github/SECURITY.md) |
| Privacy policy | [PRIVACY.md](PRIVACY.md) |
| Disclaimer | [DISCLAIMER.md](DISCLAIMER.md) |
| Terms of use (EULA) | [EULA.md](EULA.md) |
| Third-party notices | [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) |
| Code of Conduct | [CODE_OF_CONDUCT.md](.github/CODE_OF_CONDUCT.md) |

---

## License

CommandForge is licensed under the **GNU General Public License v3.0** — see
[LICENSE](LICENSE). Third-party components are listed in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

---

## AI disclosure

This project is developed by a solo author **with AI assistance**. AI tools helped write and
review code, documentation, and the command catalog; all output is human-reviewed before
release. **Non-English text in the app and docs is AI-translated** and may contain inaccuracies
— corrections are welcome. AI-assisted contributions are welcome too, **if disclosed** (see
[CONTRIBUTING.md](.github/CONTRIBUTING.md)).

---

## Trademarks

CommandForge is an independent project and is **not affiliated with, sponsored by, or endorsed
by Microsoft**. "Windows", "Microsoft", and related names are trademarks of Microsoft
Corporation. The underlying commands (DISM, SFC, winget, powercfg, netsh…) are built-in Windows
tools; CommandForge only provides a convenient interface to them. See [DISCLAIMER.md](DISCLAIMER.md).
