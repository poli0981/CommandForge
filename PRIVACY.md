# Privacy Policy — CommandForge

> This is a plain-language summary, not legal advice. Last updated: 2026-10-21.

## Summary

**CommandForge does not collect, store on any server, or transmit any of your personal data.**
The app runs entirely **locally** on your machine. The **only** network connection the app itself
makes is to GitHub, to check for and download software updates.

## Data stored on your PC (never leaves your machine)

| Data | Location | Purpose |
|---|---|---|
| Settings: theme, language, font size, safety and behavior options, log level, favorites, recent commands, window size and position, accepted-terms version | `%AppData%\CommandForge\config.json` | Remember your preferences |
| Execution history: command ID, time, exit code, result, duration | `%AppData%\CommandForge\history.json` | The History page, Run again and Undo |
| Recipes you create | `%AppData%\CommandForge\recipes.json` | Your saved command chains |
| Commands you add under *My commands* | `%AppData%\CommandForge\user-commands.json` | Your own commands |
| Saved settings profiles | `%AppData%\CommandForge\profiles.json` | Switching between profiles |
| Application logs (at most 14 daily files of up to 10 MB each) | `%AppData%\CommandForge\logs\` | Local troubleshooting |
| Backups of damaged files (`*.corrupt-<timestamp>.json`) | `%AppData%\CommandForge\` | So a damaged file is never silently lost |
| Files you export: settings, logs (`.zip`), console output | A location you choose | Only when you ask for them |
| The program itself | `%LocalAppData%\CommandForge\` | Installed app files (Velopack) |

This data stays on your device and is never sent to us or to any third party. The history stores
command IDs and results, not command output.

**Logs** record app events, errors, and a one-line summary of each command you run (its ID, exit
code and duration). They don't contain the commands' output, but they may contain machine-specific
details such as file paths, your user name or device names.

## Network connections

- **Update check:** the app contacts **GitHub** (the releases API and asset downloads) when it
  starts, if *Check for updates on startup* is on (the default), and whenever you choose **Check
  for updates** (`F5`). As with any download, GitHub sees your IP address and the request; see
  [GitHub's Privacy Statement](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement).
  We receive no data from it.
- **Links you open** — GitHub, the user guide, the license, *Report a bug* — open in your browser.
- The app makes **no other network connections**. Some **commands you choose to run** use the
  network by their nature (for example winget, DNS and connectivity tests, Windows Update); that
  traffic goes between Windows and those services, not to us.

## No telemetry / analytics

CommandForge contains **no** telemetry, analytics, advertising, crash uploading, or behavioral
tracking of any kind.

## Bug reports

*Report a bug* opens a pre-filled GitHub issue form in your browser with the app version, your
Windows version and, after a crash, the error details. **Nothing is sent until you submit the form
yourself**, and you can edit it first. If you attach exported logs, **review them before sharing**
— they may include paths, user names or device names you may not wish to publish.

## Children

The app is not directed at children and does not knowingly collect data from anyone (because it
collects no data at all).

## Changes

This policy may be updated; new versions carry an updated date. Significant changes may prompt you
to review the terms again on startup.

## Contact

- Privacy questions: **[privacy@poli0981.dev](mailto:privacy@poli0981.dev)**
- Security issues: see [SECURITY.md](.github/SECURITY.md) (security@poli0981.dev or private
  vulnerability reporting).
- Repository: https://github.com/poli0981/CommandForge
