# Privacy Policy — CommandForge

> This is a plain-language summary, not legal advice. Last updated: 2026-06-18.

## Summary

**CommandForge does not collect, store on any server, or transmit any of your personal data.**
The app runs entirely **locally** on your machine. The **only** network connection it makes is
to GitHub, to check for and download software updates.

## Data processed locally (never leaves your machine)

| Data | Location | Purpose |
|---|---|---|
| Settings (theme, language, font size, favorites, window placement, accepted-terms version) | `%AppData%\CommandForge\config.json` | Remember your preferences |
| Application logs | `%AppData%\CommandForge\logs\` | Local troubleshooting |
| Reports you generate (e.g. exported logs) | A folder you choose | Only when you ask for them |

This data stays on your device and is never sent to us or any third party.

## Network connections

- **Update check:** the app contacts **GitHub** (releases API + asset download) to see whether a
  newer version exists and to fetch it. This is subject to
  [GitHub's Privacy Statement](https://docs.github.com/site-policy). We receive no identifying
  data from it.
- The app makes **no other network connections**.

## No telemetry / analytics

CommandForge contains **no** telemetry, analytics, advertising, or behavioral tracking of any kind.

## Commands you run

When you run a command, its output is shown in the app and may be written to the **local** log
for troubleshooting. This output is never sent anywhere. If you export logs to file a bug report,
**review the contents yourself before sharing** — logs may include machine-specific details
(paths, device names) you may not wish to publish.

## Children

The app is not directed at children and does not knowingly collect data from anyone (because it
collects no data at all).

## Changes

This policy may be updated; new versions carry an updated date. Significant changes may prompt you
to review the terms again on startup.

## Contact

- Repository: https://github.com/poli0981/CommandForge
- Questions: open an issue, or use private vulnerability reporting for security matters
  (see [SECURITY.md](.github/SECURITY.md)).
