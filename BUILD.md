# Building CommandForge

## Requirements

- Windows 10 (22H2) or Windows 11
- **.NET 10 SDK** — the feature band is pinned in `global.json` (`dotnet --version` → `10.0.4xx`)
- Admin rights are only needed to **test** elevated commands, not to build.

The solution file is **`CommandForge.slnx`** (the XML solution format).

| Project | Role |
|---|---|
| `src/CommandForge.Domain` | Entities; no dependencies |
| `src/CommandForge.Application` | Use cases and ports; depends only on Domain |
| `src/CommandForge.Infrastructure` | Processes, registry, catalog, storage, Velopack, logging |
| `src/CommandForge.Wpf` | The WPF app (WPF-UI); namespace `CommandForge.Desktop` |
| `helper/CommandForge.Elevator` | The elevated helper that runs admin commands over a named pipe |
| `tests/CommandForge.Tests` | xUnit v3 tests |

## Build, run, test

```bash
dotnet restore CommandForge.slnx
dotnet build CommandForge.slnx -c Release   # must be 0 warnings (warnings are errors)
dotnet run --project src/CommandForge.Wpf
dotnet test CommandForge.slnx
```

The tests use xUnit v3 in VSTest mode (`xunit.v3.mtp-off`), so `dotnet test` and the coverlet
collector work as usual.

## Code style and analysis

```bash
dotnet format CommandForge.slnx                       # apply formatting
dotnet format CommandForge.slnx --verify-no-changes   # CI gate
```

The build treats warnings as errors and enforces the .NET analyzers, code style
(`EnforceCodeStyleInBuild`) and **SonarAnalyzer.CSharp** (the same rules SonarCloud applies),
including unused-using and unused-member rules.

## Dependencies

- Package versions live in **each `.csproj`**; SonarAnalyzer is referenced once from
  `Directory.Build.props`. Central Package Management is deliberately not used: GitHub's dependency
  graph (Dependabot alerts, dependency review, the release SBOM) doesn't read
  `Directory.Packages.props`.
- Every restore runs a **NuGet audit** of direct and transitive packages; moderate or higher
  advisories fail the build.
- Dependabot groups updates (Velopack and the `vpk` tool move together) with a cooldown.

## Publish (self-contained) + package

The Velopack CLI is pinned as a local tool in `.config/dotnet-tools.json`, at the same version as
the Velopack library:

```bash
dotnet tool restore
pwsh ./scripts/pack.ps1 -Version 2.0.0              # publish + vpk pack
pwsh ./scripts/pack.ps1 -Version 2.0.0 -SkipPack    # publish only
```

`pack.ps1` (parameters: `-Version`, `-Runtime` = `win-x64`, `-Configuration` = `Release`,
`-SkipPack`):

- runs a self-contained **ReadyToRun** publish, stamping the version so the in-app version matches
  the release;
- checks that `CommandForge.Elevator` is in the publish folder (the
  `PublishElevatorOnPublish` target in `src/CommandForge.Wpf/CommandForge.Wpf.csproj` publishes it
  alongside the app);
- runs `vpk pack` with `--runtime`, so the installer and updater helpers are native x64, and stops
  on any non-zero exit code.

It writes these artifacts to `Releases/`:

- `CommandForge-win-Setup.exe` — the installer
- `CommandForge-<version>-full.nupkg` — full package
- `CommandForge-<version>-delta.nupkg` — delta package (only when a previous full package is
  present; see [UPDATES.md](UPDATES.md))
- `releases.win.json` / `RELEASES` — the update manifest read by the app

> No Authenticode signing yet — see [SECURITY.md](.github/SECURITY.md). Don't add a signing step
> until Azure Trusted Signing (or SignPath Foundation for OSS) is set up.

## CI

All workflows pin actions to full commit SHAs and use least-privilege permissions.

| Workflow | What it does |
|---|---|
| `build.yml` | `build`: restore, `dotnet format --verify-no-changes`, Release build, tests (results and coverage uploaded). `package`: publish and `vpk pack` exactly like a release. |
| `codeql.yml` | CodeQL for C# (Windows, manual build) and for the workflows themselves (`actions`). |
| `dependency-review.yml` | Fails a PR that adds a dependency with a moderate+ vulnerability or a license outside the GPLv3-compatible allow-list. |
| `scorecard.yml` | OpenSSF Scorecard, reported to code scanning. |
| `release.yml` | Tag-triggered release — see [RELEASING.md](RELEASING.md). |
| `notify-ci-failure.yml` | Posts a Discord alert when the Build workflow fails on `main` (needs the `DISCORD_CI_WEBHOOK` secret). |

SonarCloud (Automatic Analysis, scoped by `.sonarcloud.properties`) and Snyk also check every PR.

`main` is protected by a repository ruleset: changes land through pull requests; `build`,
`package`, `Analyze (csharp)`, `Analyze (actions)` and `dependency-review` must pass; and a PR
can't introduce CodeQL errors or high-severity security alerts.

See [UPDATES.md](UPDATES.md) for how auto-update works.
