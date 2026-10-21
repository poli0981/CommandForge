# Security Policy

CommandForge runs system-level commands, some as administrator, so security is a top priority.

## Supported versions

| Version | Supported |
|---|---|
| 2.0.x (latest) | ✅ |
| < 2.0 | ❌ — please update |

Only the **latest release** receives security fixes. Please update before reporting.

## Reporting a vulnerability

**Do not open a public issue for security vulnerabilities.** Report privately in either of these ways:

- **[GitHub private vulnerability reporting](https://github.com/poli0981/CommandForge/security/advisories/new)**
  (Security → Advisories → Report a vulnerability) — preferred, or
- **email: [security@poli0981.dev](mailto:security@poli0981.dev)**.

Please include:

- the affected version (More → About in the app),
- steps to reproduce,
- the impact you observed or expect.

We aim to acknowledge reports within a reasonable time and will credit reporters who wish to be
named. Please give us a reasonable window to fix the issue before any public disclosure.

## Security model

CommandForge guards the line between your normal (non-elevated) session and administrator rights:
no file, setting, network response or other program running as you should be able to decide what
runs elevated.

- **Vetted, read-only catalog.** Every command that can run as administrator is in a catalog that
  is built into the app and reviewed by the maintainer. The app never loads catalog commands from
  files, users or the network. When the catalog loads, contradictory or unsafe records are rejected
  (for example, unknown placeholders, or a Caution/Dangerous command without a confirmation).
- **Least privilege.** The UI runs as the invoking user (`asInvoker`). Admin commands go to a
  separate helper, `CommandForge.Elevator`, which starts with one UAC prompt per session; you can
  end the session at any time. The helper accepts only a **command ID**, which it looks up in its
  own copy of the catalog — never a command line.
- **Authenticated channel.** The named pipe has a random name and an access list for your account
  only, and each end verifies the other: the app accepts only the helper process it launched, and
  the helper serves only `CommandForge.Wpf.exe` from its own folder. The helper connects with
  Identification-level impersonation and writes no files; its diagnostics travel back to the app's
  log through the pipe.
- **Trusted paths and environment.** Catalog programs resolve to absolute paths in `System32` or
  the Windows folder, never through a search of the per-user app folder. Elevated commands get an
  environment rebuilt from trusted sources (the system directory, known folders, HKLM), so per-user
  variables such as `PSModulePath`, `COR_PROFILER`, `SystemRoot` or `windir` can't redirect what an
  elevated command loads or deletes. The `{SystemDrive}`, `{SystemRoot}` and `{UserProfile}`
  placeholders in the catalog are filled from the same trusted values, and PowerShell always runs
  with `-NoProfile -NonInteractive`. The helper ignores `DOTNET_STARTUP_HOOKS`.
- **Your own commands stay unprivileged.** Commands you add under *My commands* are stored apart
  from the catalog and always run without administrator rights.
- **Transparency and confirmation.** The exact command line is shown before it runs. Caution
  commands ask first; Dangerous commands require typing the command's name or ID, and that check
  can't be turned off. A System Restore Point can be created first.
- **Containment.** Commands whose output is captured run in a kill-on-close job object, so closing
  or crashing the app doesn't leave them running.
- **No telemetry.** The only network connection the app makes is the GitHub update check (see
  [PRIVACY.md](../PRIVACY.md)).

### Known limitations

- **Per-user installation.** Velopack installs the app under `%LocalAppData%\CommandForge`, a
  folder your account can write to. Malware already running as you could replace the app's files —
  including the elevated helper — and wait for you to approve the next UAC prompt. This applies to
  every per-user app (UAC does not separate programs running as the same user), and since releases
  aren't Authenticode-signed yet, the UAC prompt can't show a verified publisher either. Keep
  untrusted software off your account, and approve the prompt only right after you started an
  admin command and it names `CommandForge.Elevator`.
- **winget** is an App Execution Alias in `%LocalAppData%\Microsoft\WindowsApps`, another per-user
  folder. The catalog's winget commands start it from there, including the ones that run as
  administrator, so the same caveat applies.
- **Commands do what they say.** Commands you choose to run can change system settings, use the
  network (winget, DNS tests…) or delete data by design; read the preview and see
  [DISCLAIMER.md](../DISCLAIMER.md).

## Distribution integrity

Releases are **not Authenticode-signed yet**, so SmartScreen may warn about an unknown publisher
and some antivirus engines may show false positives (common for admin tools). Instead:

- Every release is built by GitHub Actions from a tagged commit and carries **build-provenance
  attestations** for the installer and packages. Verify a download with
  `gh attestation verify <file> --repo poli0981/CommandForge`.
- **SHA-256 checksums** (`checksums.txt`), an **SPDX SBOM** (`sbom.spdx.json`) and **VirusTotal**
  scan links come with every release.
- The code is **not obfuscated** (obfuscation is a leading cause of false positives), and the
  source is public under the GPLv3.
- CI runs **CodeQL** (C# and GitHub Actions), **dependency review**, **OpenSSF Scorecard**,
  **SonarCloud** and **Snyk**; every restore audits direct and transitive NuGet packages;
  Dependabot keeps dependencies and the SHA-pinned actions current; `main` only accepts pull
  requests that pass the required checks.

Code signing (Azure Trusted Signing, or SignPath Foundation for open source) is planned for a
future release.

## Malicious contributions

Contributions suspected of containing malicious code are closed and may result in a ban — see
[CONTRIBUTING.md](CONTRIBUTING.md). Detection equals a ban, no exceptions.
