# Releasing CommandForge

Releases are automated by [`release.yml`](.github/workflows/release.yml), triggered by pushing a
SemVer tag.

## Steps

1. Update `docs/Changelog.md` (internal) and make sure `main` is green.
2. Choose a SemVer version `X.Y.Z`.
3. Tag and push:
   ```bash
   git tag v0.3.0
   git push origin v0.3.0
   ```
4. The **Release** workflow runs on `windows-latest` and:
   - resolves the version from the tag (`vX.Y.Z` → `X.Y.Z`),
   - installs `vpk`,
   - **downloads the previous release** (`vpk download github`) so a delta can be computed
     (skipped/ignored on the very first release),
   - publishes self-contained and packages with Velopack (`scripts/pack.ps1`, which stamps the
     assembly version),
   - **publishes the GitHub Release** with all `Releases/` assets (`vpk upload github --publish`),
   - generates **`checksums.txt`** (SHA-256) and attaches it,
   - if `VT_API_KEY` is set, submits `Setup.exe` + `*-full.nupkg` to **VirusTotal** and appends the
     scan links to the release notes.
5. Edit the GitHub Release notes if needed (changelog highlights). The SHA-256 + VirusTotal info is
   already attached.

You can also run the workflow manually (**Actions → Release → Run workflow**) with a `version`
input; it tags `vX.Y.Z` via the upload step.

## Required repository secrets

| Secret | Used for | Required |
|---|---|---|
| `GITHUB_TOKEN` | Create the release & upload assets | automatic |
| `VT_API_KEY` | VirusTotal scan + links | optional (step skipped if unset) |
| `DISCORD_RELEASES_WEBHOOK` (or `DISCORD_REPO_WEBHOOK`) | Release announcement to Discord | optional |
| `DISCORD_PING_ROLE_ID` | Role ping on stable releases | optional |
| `DISCORD_CI_WEBHOOK` | Build-failure alerts to Discord | optional |

> `poli0981` is a user account (not an org), so secrets are set **per repository**
> (Settings → Secrets and variables → Actions).

## Code signing

Not done yet. Releases ship unsigned; users may see a SmartScreen "unknown publisher" warning.
Mitigations: SHA-256 + VirusTotal (above), no obfuscation, public source. When Azure Trusted
Signing (or SignPath Foundation) is configured, add a signing step after `dotnet publish` and
before `vpk pack`, and update [SECURITY.md](.github/SECURITY.md).
