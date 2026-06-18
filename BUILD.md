# Building CommandForge

## Requirements

- Windows 10 (22H2) or Windows 11
- **.NET 10 SDK** (`dotnet --version` → `10.0.x`)
- For packaging: the **Velopack CLI** — `dotnet tool install -g vpk`
- Admin rights are only needed to **test** elevated commands, not to build.

The solution file is **`CommandForge.slnx`** (the XML solution format).

## Build, run, test

```bash
dotnet restore CommandForge.slnx
dotnet build CommandForge.slnx -c Release   # must be 0 warnings (warnings are errors)
dotnet run --project src/CommandForge.Wpf
dotnet test CommandForge.slnx
```

## Code style

```bash
dotnet format CommandForge.slnx                       # apply formatting
dotnet format CommandForge.slnx --verify-no-changes   # CI gate
```

The build enforces analyzers as errors (`TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`),
including unused-using/unused-member rules.

## Publish (self-contained) + package

```bash
# One-step helper (publish + vpk pack):
pwsh ./scripts/pack.ps1 -Version 0.3.0
```

`pack.ps1` runs a self-contained `win-x64` publish (stamping the assembly version so the in-app
version matches the release) and then `vpk pack`. It writes these artifacts to `Releases/`:

- `CommandForge-win-Setup.exe` — the installer
- `CommandForge-<version>-full.nupkg` — full package
- `CommandForge-<version>-delta.nupkg` — delta package (only when a previous full is present)
- `releases.win.json` / `RELEASES` — the update manifest read by the app

The `CommandForge.Elevator` helper is published into the same folder automatically (see the
`PublishElevatorOnPublish` target in `src/CommandForge.Wpf/CommandForge.Wpf.csproj`).

> No code signing yet — see [SECURITY.md](.github/SECURITY.md). Don't add a signing step until
> Azure Trusted Signing (or SignPath Foundation for OSS) is set up.

## CI

- **`build.yml`** — restore + `dotnet format --verify-no-changes` + Release build + tests on every
  PR/push to `main` (Windows).
- **`codeql.yml`** — CodeQL C# analysis (Windows, builds `CommandForge.slnx`).
- **`release.yml`** — see [RELEASING.md](RELEASING.md).

See [UPDATES.md](UPDATES.md) for how auto-update works.
