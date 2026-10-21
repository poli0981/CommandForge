# CommandForge

> Run powerful Windows maintenance commands from a clean, categorized GUI — no need to
> memorize syntax.

CommandForge is an open-source Windows desktop app that wraps common system commands
(winget, DISM, SFC, God Mode, registry tweaks, network reset, power plans, diagnostics…)
in a visual, categorized interface. Every command is shown to you **before** it runs, with a
clear danger level, a confirmation for anything risky, and an optional System Restore Point.

- **Windows 10 (22H2) and Windows 11**, x64.
- **Open source, GPLv3.**
- **Offline-first. No telemetry.** The only network connection the app makes is to GitHub, to
  check for updates.
- Installed and auto-updated with [Velopack](https://velopack.io/) (per-user, no UAC for updates).

> Built by a solo developer with AI assistance — see the [Disclaimer](DISCLAIMER.md) and the
> AI disclosure below.

---

## Features

- **261 vetted commands in 12 categories** — System & Maintenance, Updates & Packages, Tweaks &
  Shortcuts, Network, Cleanup, Power, System Info, Diagnostics, Security & Privacy, Developer,
  Storage, and Services & Startup. Commands that don't apply to your Windows build are hidden.
- **Fluent design** — a Windows 11-style window (Mica, navigation pane, in-window dialogs) that
  follows your light/dark theme, accent color and High Contrast. It is resizable and remembers its
  size and position.
- **Find anything fast** — accent-insensitive search (`Ctrl+F`), filters by danger level and admin
  rights, favorites, and a Command Palette over the whole catalog (`Ctrl+K`).
- **Safe by design** — the catalog is a read-only part of the app, reviewed by the maintainer;
  nothing outside it ever runs as administrator. Admin commands run in a separate elevated helper
  (one UAC prompt per session, which you can end at any time).
- **See before you run** — the exact command line is previewed with paths filled in. `Caution`
  commands ask first; `Dangerous` ones require typing the command's name or ID, and that check
  can't be turned off. A System Restore Point can be created first.
- **Live console** — output streams in real time and stays readable on non-English Windows
  (UTF-16, UTF-8 and OEM code pages are decoded); cancel, copy, or save it to a file.
  Restart-required is detected from the exit code (`3010`).
- **History, undo and recipes** — run past commands again, undo reversible tweaks (with a registry
  before/after diff), and chain commands into recipes that confirm every risky step.
- **My commands** — save your own commands. They are kept apart from the catalog and always run
  without admin rights.
- **Five languages** — English, Tiếng Việt, 日本語, 简体中文 and Español, switchable without a
  restart.
- **Diagnostics** — Log Viewer, Debug panel, local logs with retention, and a crash dialog with a
  "Report a bug" flow that never sends anything by itself.

New here? Read the **[user guide](USAGE.md)**.

---

## Install

1. Download **`CommandForge-win-Setup.exe`** from the
   [latest release](https://github.com/poli0981/CommandForge/releases/latest).
2. Run it. The app installs per-user (no admin rights needed) and updates itself from future
   releases.

Already on 1.x? The app offers 2.0 as a normal update; your settings, favorites, history, recipes
and commands carry over.

### "Unknown publisher" and verifying your download

CommandForge is **not Authenticode-signed yet** (see [SECURITY.md](.github/SECURITY.md)), so
SmartScreen may warn about an unknown publisher, and some antivirus engines may flag it (common
for system-administration tools). To proceed: **More info → Run anyway**.

Every release is built by GitHub Actions from a tagged commit. You can check your download first:

- **Build provenance** — with the [GitHub CLI](https://cli.github.com/), confirm the file was built
  by this repository's release workflow:

  ```powershell
  gh attestation verify .\CommandForge-win-Setup.exe --repo poli0981/CommandForge
  ```

- **SHA-256** — compare with `checksums.txt` on the release page (case doesn't matter):

  ```powershell
  (Get-FileHash .\CommandForge-win-Setup.exe -Algorithm SHA256).Hash
  ```

- The release notes link a **VirusTotal** scan, and each release ships an SPDX **SBOM**
  (`sbom.spdx.json`).

---

## Build from source

Requires Windows 10 22H2 / 11 and the **.NET 10 SDK** (the exact band is pinned in `global.json`).

```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/CommandForge.Wpf
dotnet test
```

See [BUILD.md](BUILD.md) for packaging and CI, and [UPDATES.md](UPDATES.md) for how updates work.

---

## Documentation

| Topic | File |
|---|---|
| User guide | [USAGE.md](USAGE.md) |
| Release history | [CHANGELOG.md](CHANGELOG.md) |
| Build, test, publish, release | [BUILD.md](BUILD.md) · [RELEASING.md](RELEASING.md) |
| Update mechanism (Velopack) | [UPDATES.md](UPDATES.md) |
| Contributing | [CONTRIBUTING.md](.github/CONTRIBUTING.md) |
| Security policy & disclosure | [SECURITY.md](.github/SECURITY.md) |
| Privacy policy | [PRIVACY.md](PRIVACY.md) |
| Disclaimer | [DISCLAIMER.md](DISCLAIMER.md) |
| Terms of use (EULA) | [EULA.md](EULA.md) |
| Third-party notices | [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) |
| Code of Conduct | [CODE_OF_CONDUCT.md](.github/CODE_OF_CONDUCT.md) |
| Maintainers | [MAINTAINERS.md](MAINTAINERS.md) |

---

## Contact

Bugs and feature requests go to [GitHub Issues](https://github.com/poli0981/CommandForge/issues),
and questions to [Discussions](https://github.com/poli0981/CommandForge/discussions). For
everything else:

| Topic | Address |
|---|---|
| Security vulnerabilities (private) | security@poli0981.dev, or [private vulnerability reporting](https://github.com/poli0981/CommandForge/security/advisories/new) |
| Privacy | privacy@poli0981.dev |
| Terms, licensing and other legal questions | legal@poli0981.dev |
| Copyright questions and permissions | copyright@poli0981.dev |
| DMCA notices | dmca@poli0981.dev |
| Other takedown requests (trademark, impersonation, unlawful content) | takedown@poli0981.dev |
| Code, contributions and Code of Conduct reports | code@poli0981.dev |
| Collaboration | collab@poli0981.dev |
| Press | press@poli0981.dev |
| Sponsorship | sponsor@poli0981.dev |

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
