# Third-Party Notices

CommandForge (licensed under GPLv3) bundles or depends on the third-party components below.
Each remains under its own license; all are compatible with GPLv3. Versions reflect the current
build — see the `.csproj` files for the authoritative versions.

## NuGet packages (runtime)

| Component | Version | License | Project |
|---|---|---|---|
| MaterialDesignThemes (+ MaterialDesignColors) | 5.3.2 | MIT | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | https://github.com/CommunityToolkit/dotnet |
| Microsoft.Extensions.Hosting | 10.0.9 | MIT | https://github.com/dotnet/runtime |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.9 | MIT | https://github.com/dotnet/runtime |
| Serilog | 4.3.1 | Apache-2.0 | https://github.com/serilog/serilog |
| Serilog.Extensions.Hosting | 10.0.0 | Apache-2.0 | https://github.com/serilog/serilog-extensions-hosting |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 | https://github.com/serilog/serilog-sinks-file |
| Serilog.Sinks.Async | 2.1.0 | Apache-2.0 | https://github.com/serilog/serilog-sinks-async |
| Velopack | 1.2.0 | MIT | https://github.com/velopack/velopack |
| System.Text.Json | (bundled with .NET 10) | MIT | https://github.com/dotnet/runtime |

## Build-time only (not redistributed)

| Component | Version | License | Project |
|---|---|---|---|
| Microsoft.Windows.CsWin32 | 0.3.296 | MIT | https://github.com/microsoft/CsWin32 |

## Runtime & platform

- **.NET 10** and **WPF** — MIT — https://github.com/dotnet/runtime, https://github.com/dotnet/wpf

## Fonts & icons

- **Material Design Icons** (via MaterialDesignThemes `PackIcon`) — Apache-2.0 / SIL OFL 1.1 —
  https://github.com/Templarian/MaterialDesign
- **Cascadia Code / Cascadia Mono** (console/monospace, referenced by name with fallbacks; not
  bundled) — SIL OFL 1.1 — https://github.com/microsoft/cascadia-code
- **Segoe UI / Segoe UI Variable** — Windows system font (not bundled).

## AI disclosure

Portions of this project's code, documentation, and command catalog were produced with AI
assistance and human review. See the AI disclosure in [README.md](README.md) and
[DISCLAIMER.md](DISCLAIMER.md).
