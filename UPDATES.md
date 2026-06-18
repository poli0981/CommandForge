# How updates work (Velopack)

CommandForge installs and updates itself with [Velopack](https://velopack.io/). Updates are
**per-user** and need **no UAC**.

## For users

1. Download `CommandForge-win-Setup.exe` from the
   [latest release](https://github.com/poli0981/CommandForge/releases) and run it. It installs
   per-user under `%LocalAppData%\CommandForge`.
2. After that, the app checks GitHub Releases for newer versions (on startup if enabled, or via
   **Help → Check for updates** / `F5`) and updates itself. When a newer version exists it downloads
   the update (a small **delta** when possible) and applies it on restart.

The only network connection involved is to GitHub (see [PRIVACY.md](PRIVACY.md)). Update errors are
mapped to clear messages: offline, not found (404), rate limited (403/429), and server error (5xx).

## How it works under the hood

- The app calls `VelopackApp.Build().Run()` at startup to handle install/update/uninstall hooks.
- It checks for updates via `UpdateManager(new GithubSource("https://github.com/poli0981/CommandForge", null, prerelease: false))`.
- Auto-update only works for an **installed** build (not a dev/portable run); the app detects this
  and hides update prompts otherwise.

## Release artifacts and the delta mechanism

Each release (see [RELEASING.md](RELEASING.md)) uploads the contents of `Releases/`:

| Asset | Purpose |
|---|---|
| `CommandForge-win-Setup.exe` | First-time installer |
| `CommandForge-<version>-full.nupkg` | Full package (fallback / first install of a version) |
| `CommandForge-<version>-delta.nupkg` | Delta from the previous version (small download) |
| `releases.win.json` / `RELEASES` | Update manifest the client reads |
| `checksums.txt` | SHA-256 of the assets (manual verification) |

**First release (e.g. `v0.3.0`):** there is no previous version, so only a **full** package +
`Setup.exe` + manifest are produced and uploaded.

**Subsequent releases (e.g. `v0.3.1`):** Velopack needs the **previous** `-full.nupkg` present
while packing to compute the delta. The release workflow does this automatically by running
`vpk download github` before `vpk pack`, so each release after the first produces **both** a
`-full.nupkg` and a `-delta.nupkg`. Clients already on `0.3.0` download only the small
`-delta.nupkg`; the full package remains the fallback.

> If you ever build a release **locally**, run `vpk download github --repoUrl <repo>` first so the
> prior full package is available for the delta — otherwise only a full package is produced.
