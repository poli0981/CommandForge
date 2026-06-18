# Security Policy

CommandForge runs system-level commands, some elevated, so security is a top priority.

## Supported versions

Only the **latest release** receives security fixes. Please update before reporting.

## Reporting a vulnerability

**Do not open a public issue for security vulnerabilities.**

Use **[GitHub private vulnerability reporting](https://github.com/poli0981/CommandForge/security/advisories/new)**
(Security → Advisories → Report a vulnerability). Include:

- affected version (Settings → About),
- steps to reproduce,
- impact.

We aim to acknowledge reports within a reasonable time and will credit reporters who wish to be
named. Please give us a reasonable window to fix before any public disclosure.

## Security model (summary)

- **Vetted, read-only catalog.** Every runnable command is in an embedded, maintainer-reviewed
  catalog. The app never loads arbitrary commands from files/users/network and self-elevates.
- **Least privilege.** The UI runs `asInvoker` (non-elevated). Only a separate `Elevator` helper
  runs as Administrator, and only when needed (one UAC prompt per session). The Elevator accepts
  only a `commandId` validated against its own embedded catalog — never an arbitrary command
  string over the pipe.
- **Transparency.** The exact command line is shown before it runs.
- **No telemetry.** The only network call is the GitHub update check.

## Code signing & distribution integrity

Releases are **not code-signed yet**, so SmartScreen may warn "unknown publisher" and some AV
engines may show false positives (common for admin tools). Mitigations:

- We **do not obfuscate** (obfuscation is a leading cause of AV false positives).
- Every release publishes **SHA-256 checksums** (`checksums.txt`) and a **VirusTotal** link in the
  release notes.
- The source is public (GPLv3) and CI runs **CodeQL** on every change.

Code signing (Azure Trusted Signing, or SignPath Foundation for OSS) is planned for a future
release.

## Malicious contributions

Contributions suspected of containing malicious code are closed and may result in a ban — see
[CONTRIBUTING.md](CONTRIBUTING.md). Detection equals a ban, no exceptions.
