# How updates work (Velopack)

CommandForge installs and updates itself with [Velopack](https://velopack.io/). Updates are
**per-user** and need **no UAC**.

## For users

1. Download `CommandForge-win-Setup.exe` from the
   [latest release](https://github.com/poli0981/CommandForge/releases/latest) and run it. It
   installs per-user under `%LocalAppData%\CommandForge`.
2. After that, the app checks GitHub Releases for newer versions — on startup if **Settings →
   Updates → Check for updates on startup** is on (the default), or when you choose **More (⋯) →
   Check for updates** or press `F5`.
3. When a newer version exists, choose **Download & restart**. The app downloads the update (a
   small **delta** when possible), then restarts into the new version.

Good to know:

- Closing the update dialog — including with its **X** button — cancels the download.
- The app never restarts in the middle of a command: if one is running, finish or cancel it first.
  If you start a command while the update downloads, the restart waits; choose **Download &
  restart** again when the command is done.
- Before applying an update, the app ends the admin session, because the elevated helper runs
  from the folder that Velopack replaces.
- Errors are shown in plain words: offline or timed out, no release feed found (404), rate limited
  (403/429), and GitHub server error (5xx).

The only network connection involved is to GitHub (see [PRIVACY.md](PRIVACY.md)).

## How it works under the hood

- `Program.Main` calls `VelopackApp.Build().Run()` before WPF starts, to handle the
  install/update/uninstall hooks.
- The app checks for updates via
  `UpdateManager(new GithubSource("https://github.com/poli0981/CommandForge", null, prerelease: false))`.
- Download and apply are separate steps, so the app can end the admin session and check for a
  running command just before it restarts.
- Auto-update works for the installed app and for the portable zip from the releases page (both
  carry Velopack's `Update.exe`). A build you run from source can't update; the app detects this
  and says so instead of offering an update.

## Release artifacts and the delta mechanism

Each release (see [RELEASING.md](RELEASING.md)) uploads the contents of `Releases/`:

| Asset | Purpose |
|---|---|
| `CommandForge-win-Setup.exe` | First-time installer |
| `CommandForge-win-Portable.zip` | The app without an installer; extract it anywhere and it updates itself in place (settings and logs still go to `%AppData%\CommandForge`) |
| `CommandForge-<version>-full.nupkg` | Full package (fallback / first install of a version) |
| `CommandForge-<version>-delta.nupkg` | Delta from the previous version (small download) |
| `releases.win.json` / `RELEASES` | Update manifest the client reads |
| `checksums.txt` | SHA-256 of the assets (manual verification) |
| `sbom.spdx.json` | SPDX software bill of materials |

The installer, the portable zip and the packages also have build-provenance attestations (see the
[README](README.md#unknown-publisher-and-verifying-your-download)).

Velopack needs the **previous** `-full.nupkg` present while packing to compute a delta. The
release workflow runs `vpk download github` before `vpk pack`, so every release after the first
produces **both** a `-full.nupkg` and a `-delta.nupkg`. Clients on the previous version (for
example 1.5.1 when 2.0.0 ships) download only the delta; the full package remains the fallback. The very first release had no previous version, so it shipped only a full package.

> If you ever build a release **locally**, run `dotnet vpk download github --repoUrl <repo>`
> first so the prior full package is available for the delta — otherwise only a full package is
> produced.
