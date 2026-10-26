# Third-Party Notices

CommandForge (licensed under GPLv3) bundles or depends on the third-party components below.
Each remains under its own license; every component shipped with the app is compatible with
GPLv3. Versions reflect the current build — see the `.csproj` files for the authoritative
versions.

## NuGet packages shipped with the app

| Component | Version | License | Project |
|---|---|---|---|
| WPF-UI (+ WPF-UI.Abstractions) | 4.3.0 | MIT | https://github.com/lepoco/wpfui |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | https://github.com/CommunityToolkit/dotnet |
| Microsoft.Extensions.DependencyInjection (+ .Abstractions) | 10.0.14 | MIT | https://github.com/dotnet/runtime |
| Serilog | 4.4.0 | Apache-2.0 | https://github.com/serilog/serilog |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 | https://github.com/serilog/serilog-sinks-file |
| Serilog.Sinks.Async | 2.1.0 | Apache-2.0 | https://github.com/serilog/serilog-sinks-async |
| Velopack (library, installer and update helpers) | 1.2.161 | MIT | https://github.com/velopack/velopack |
| System.Text.Json and the rest of the .NET libraries | (bundled with .NET 10) | MIT | https://github.com/dotnet/runtime |

WPF-UI incorporates code from VirtualizingWrapPanel, dotnet/wpf and microsoft-ui-xaml (all MIT)
and the Fluent System Icons (below); see the `ThirdPartyNotices.txt` in the WPF-UI package.

## Build and test tools (not redistributed)

| Component | Version | License | Project |
|---|---|---|---|
| Microsoft.Windows.CsWin32 | 0.3.367 | MIT | https://github.com/microsoft/CsWin32 |
| SonarAnalyzer.CSharp | 10.35.0.4138 | SONAR Source-Available License v1.0 | https://github.com/SonarSource/sonar-dotnet |
| dotnet-sonarscanner (SonarScanner for .NET, CI only) | 12.0.0 | LGPL-3.0 | https://github.com/SonarSource/sonar-scanner-msbuild |
| vpk (Velopack CLI) | 1.2.161 | MIT | https://github.com/velopack/velopack |
| xunit.v3 (`xunit.v3.mtp-off`) | 4.0.2 | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.runner.visualstudio | 4.0.1 | Apache-2.0 | https://github.com/xunit/visualstudio.xunit |
| Microsoft.NET.Test.Sdk | 18.11.0 | MIT | https://github.com/microsoft/vstest |
| coverlet.collector | 10.1.0 | MIT | https://github.com/coverlet-coverage/coverlet |

## Runtime & platform

- **.NET 10** and **WPF** — MIT — https://github.com/dotnet/runtime, https://github.com/dotnet/wpf

## Fonts & icons

- **Fluent System Icons** (bundled inside WPF-UI; used for every icon in the app) — MIT,
  © Microsoft Corporation — https://github.com/microsoft/fluentui-system-icons
- **Cascadia Mono** (console and code text, referenced by name with Consolas / Courier New
  fallbacks; not bundled) — SIL OFL 1.1 — https://github.com/microsoft/cascadia-code
- **Segoe UI Variable / Segoe UI** — Windows system fonts (not bundled).

## AI disclosure

Portions of this project's code, documentation, and command catalog were produced with AI
assistance and human review. See the AI disclosure in [README.md](README.md) and
[DISCLAIMER.md](DISCLAIMER.md).
