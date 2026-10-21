# Releasing CommandForge

Releases are automated by [`release.yml`](.github/workflows/release.yml), triggered by pushing a
SemVer tag.

## Steps

1. Make sure `main` is green and contains everything for the release.
2. Choose a SemVer version `X.Y.Z`. In a pull request, move the `[Unreleased]` notes in
   [CHANGELOG.md](CHANGELOG.md) under `## [X.Y.Z] — <date>`, add the compare link at the bottom,
   and set `<Version>` in `Directory.Build.props` (the dev-build default; the release build stamps
   the tag's version anyway). Merge it.
3. Tag the merge commit on `main` with a **signed, annotated** tag and push it:

   ```bash
   git switch main && git pull
   git tag -s vX.Y.Z -m "CommandForge X.Y.Z"
   git push origin vX.Y.Z
   ```

   The `release tags` ruleset protects `v*` tags: once pushed, a release tag can't be moved or
   deleted (only a repository admin can bypass this).
4. The **Release** workflow runs on `windows-latest` and:
   - resolves the version from the tag (`vX.Y.Z` → `X.Y.Z`);
   - restores the pinned `vpk` tool and **downloads the previous release** so a delta package can
     be computed;
   - publishes and packages with Velopack (`scripts/pack.ps1`);
   - creates **build-provenance attestations** for `Setup.exe` and the `.nupkg` files;
   - uploads everything as a **draft** GitHub Release, attaches the **SPDX SBOM**
     (`sbom.spdx.json`) and **`checksums.txt`** (SHA-256), then **publishes** it as the latest
     release — installed copies only see the update once every asset is in place;
   - if `VT_API_KEY` is set, submits `Setup.exe` and the full package to **VirusTotal** and appends
     the scan links to the release notes;
   - calls the shared announcer (`poli0981/.github`) to post the release to Discord.
5. Check the release page: the assets are all there, and the attestation verifies:

   ```bash
   gh release download vX.Y.Z --pattern "*-Setup.exe"
   gh attestation verify CommandForge-win-Setup.exe --repo poli0981/CommandForge
   ```

   Edit the release notes if needed (changelog highlights). The SHA-256 and VirusTotal details are
   already attached.

You can also run the workflow manually (**Actions → Release → Run workflow**) with a `version`
input; the upload step then creates the `vX.Y.Z` tag.

## Repository secrets

| Secret | Used for | Required |
|---|---|---|
| `GITHUB_TOKEN` | Create the release, upload assets, read the SBOM | automatic |
| `VT_API_KEY` | VirusTotal scan + links | optional (step skipped if unset) |
| `DISCORD_RELEASES_WEBHOOK` (or `DISCORD_REPO_WEBHOOK`) | Release announcement to Discord | optional |
| `DISCORD_PING_ROLE_ID` | Role ping on stable releases | optional |
| `DISCORD_CI_WEBHOOK` | Build-failure alerts to Discord | optional |

> `poli0981` is a user account (not an org), so secrets are set **per repository**
> (Settings → Secrets and variables → Actions).

## Code signing

Not done yet: releases ship without an Authenticode signature, so users may see a SmartScreen
"unknown publisher" warning. Mitigations: build-provenance attestations, SHA-256 checksums, an
SBOM, VirusTotal, no obfuscation, and public source. When Azure Trusted Signing (or SignPath
Foundation) is configured, add a signing step between `dotnet publish` and `vpk pack` in
`scripts/pack.ps1`, and update [SECURITY.md](.github/SECURITY.md).
