# CommandForge user guide

CommandForge runs Windows maintenance commands for you: pick a command, read what it will do, and
run it. This guide covers version 2.0. For installing and verifying downloads, see the
[README](README.md#install).

- [First start](#first-start)
- [The window](#the-window)
- [Finding a command](#finding-a-command)
- [Running a command](#running-a-command)
- [Home](#home)
- [History and undo](#history-and-undo)
- [Recipes](#recipes)
- [My commands](#my-commands)
- [Settings](#settings)
- [Log viewer and Debug panel](#log-viewer-and-debug-panel)
- [Keyboard shortcuts](#keyboard-shortcuts)
- [Updates](#updates)
- [Your data and uninstalling](#your-data-and-uninstalling)
- [Troubleshooting](#troubleshooting)

## First start

The first time you open CommandForge, it asks you to accept its terms:

1. Pick your language in the list above the terms. English, Tiếng Việt, 日本語, 简体中文 and
   Español are available, and you can change it later in Settings.
2. Read the summaries on the **EULA**, **GPLv3**, **Disclaimer** and **Privacy** tabs. **Read the
   full text on GitHub** opens the complete documents.
3. Tick **I have read and agree to all of the terms**, then choose **Continue**.

You'll be asked again only if the terms change.

## The window

- **Title bar**
  - The search box, **Search all commands (Ctrl+K)**, opens the Command Palette.
  - **Admin session active** appears while administrator commands can run without a new UAC
    prompt. Open it to **End admin session**.
  - **More (⋯)** holds the Command Palette, **Check for updates**, **Create System Restore
    Point**, **Export logs (.zip)**, the config and log folders, **Portable info**, **Keyboard
    shortcuts**, **User guide**, **Report a bug**, **Terms & Privacy** and **About CommandForge**.
- **Navigation pane** (left)
  - Home, Favorites, History, Recipes and My commands.
  - **Command catalog**, with its 12 categories and the number of commands in each.
  - Log viewer, Debug panel and Settings, at the bottom.
  - `Ctrl+B` shows or hides the pane.
- The window can be resized (down to a minimum size) and maximized. It reopens where you left it.

## Finding a command

- **Browse**: open **Command catalog** or one of its categories. Commands that don't apply to your
  version of Windows are hidden.
- **Search**: type in the search box above the list (`Ctrl+F`). Search ignores case and accents
  ("don dep" finds "Dọn dẹp") and matches titles, descriptions and tags. For everyday phrases such
  as "clean disk", a **Did you mean:** suggestion offers the catalog's own word.
- **Filter**: the chips above the list show only **Safe**, **Caution** or **Dangerous** commands,
  or **Admin only**. **Clear filters** resets them.
- **Favorites**: select the star on a command to pin it. Pinned commands appear on the
  **Favorites** page and on Home.
- **Command Palette** (`Ctrl+K`): search the whole catalog from anywhere. Use `↑`/`↓` to move and
  `Enter` to open the command.

## Running a command

Select a command to see its details:

- its description and danger level (**Safe**, **Caution** or **Dangerous**);
- whether it **Requires admin** or **May require restart**;
- the **Estimated time**;
- **Command to run** — the exact command line, with paths filled in. **Copy** copies it.

Choose **Run** to start it. A command that can be undone also shows **Revert**.

### Danger levels and confirmations

| Level | Meaning | Before it runs |
|---|---|---|
| **Safe** | No risk of losing data: read-only queries, opening a tool or settings page, or a small preference change you can undo the same way. | Runs right away. |
| **Caution** | Changes system state (services, registry, caches, boot-time checks). Recoverable, but undoing it may take effort. | Asks you to confirm, unless you turned that off in Settings. |
| **Dangerous** | May lose data, remove a rollback option or leave Windows unstable. | You must type the command's name (or the ID shown) before **Run** appears. This can't be turned off. |

In every confirmation, **Cancel** is the default and `Esc` closes the dialog.

### System Restore Points

For risky administrator commands, the confirmation offers **Create a System Restore Point first
(recommended)**.

- Windows allows only one restore point every 24 hours. If a recent one exists, CommandForge says
  it skipped creating a new one.
- If creating it fails, you're asked whether to run the command without one. **No** is the
  default.

You can also create one at any time from **More → Create System Restore Point**.

### Administrator commands

The first administrator command of a session shows a Windows UAC prompt for
**CommandForge.Elevator**, and the console shows **Waiting for administrator permission…**.

- If you approve, the elevated helper stays open, so later admin commands run without another
  prompt. **Admin session active** appears in the title bar.
- **End admin session** closes the helper when you're done; the next admin command asks again.
- If you decline the prompt, the command doesn't run.

The rest of CommandForge never runs as administrator.

### The console

Output appears live below the details. Drag the splitter to give it more room.

- **Cancel** stops the running command.
- **Copy all output** and **Save output to a file** keep a copy of what it printed.
- When the command ends, the console shows the result (**Success**, **Failed** or **Cancelled**),
  the exit code and how long it took.
- Very long output keeps the newest 10,000 lines; a note tells you how many earlier lines aren't
  shown.
- Registry tweaks list the affected values before and after the run.
- While a command runs, the taskbar button shows progress. If you moved to another page when it
  finishes, a notification tells you the result; if CommandForge is in the background, its
  taskbar button also flashes.

### Restarting Windows

When a command reports that a restart is needed (exit code 3010, or a command that always needs
one), the console shows **Restart now**. It asks before restarting and isn't available while a
command is running.

## Home

Home shows:

- **System status**: Windows build, memory, system drive space and uptime.
- **Maintenance suggestions**: for example, Disk Cleanup when the system drive is low on space, or
  a restart after a week of uptime. **Open** shows the suggested command.
- Your **Recent commands** and **Favorites**.

## History and undo

**History** lists every command you ran, with the time, result and duration.

- **Run again** repeats a command, with the same confirmations as the first time.
- **Undo** appears for commands that have a vetted revert command (for example, turning a tweak
  back off).
- **Clear history** deletes the list after asking.

Run again and Undo are unavailable while another command is running.

## Recipes

A recipe runs several catalog commands in order.

1. Open **Recipes** and choose **New**, then give the recipe a name.
2. Use **Add a command…** and **Add** to append steps. Reorder them with **Move up** and **Move
   down**, or use **Remove step**.
3. Choose **Save**, then **Run recipe**.

When a recipe runs:

- You first see an overview of its steps and confirm (**No** is the default).
- Each Caution or Dangerous step then asks exactly as it would on its own.
- If any step is a risky administrator command, a restore point is offered once, before the
  first step.
- Administrator steps share one admin session, so you get a single UAC prompt.
- The recipe stops if a step fails, and pauses if a step needs a restart. **Cancel** stops it.
- Steps that aren't available on this PC stay in the recipe but are skipped.

## My commands

Under **My commands** you can save your own command lines, such as a `ping` you run often.

- They are **not part of the vetted catalog** and always run **without administrator rights**.
  You alone are responsible for what they do.
- Choose **New**, fill in **Name**, **Executable** (for example `cmd` or `powershell`) and
  **Arguments**, then choose **Save**.
- **Run** shows the exact command line and asks before running it.
- With `cmd`, `/c` is added for you, so `cmd` + `ping example.com` runs `cmd /c ping example.com`.

## Settings

| Section | Options |
|---|---|
| **Appearance** | Theme (System, Light, Dark), Language, Font size (Small, Medium, Large). |
| **Behavior & Safety** | Collapse the navigation pane by default; show Admin/Restart badges in lists; confirm before Caution commands; confirm before Dangerous commands (always on); tick "create a restore point" by default for risky admin commands; auto-scroll the console; warn before closing while a command is running. |
| **Updates** | Check for updates on startup, the current version, the last check and **Check now**. |
| **Backup & profiles** | **Export…** / **Import…** your appearance, behavior and favorites as a `.json` file, and save named profiles (for example "Gaming PC" or "Office") that you can **Apply** or **Delete**. |
| **Logs** | Log level, log size, **Open log folder**, **Export logs (.zip)**, **Delete old logs**. |
| **Advanced** | Run mode (Installed or Portable / dev), the config folder, the Debug panel, and **Reset to defaults** (keeps your accepted terms and logs). |
| **About** | Version, license and GitHub links. |

Theme, language and font size change immediately; no restart needed.

## Log viewer and Debug panel

- **Log viewer** (`Ctrl+L`) shows this session's log.
  - Filter by level or text, and turn auto-scroll on or off.
  - **Open log folder** or **Export logs (.zip)** to share them. Review an export before you share
    it.
  - **Clear** empties the list shown.
- **Debug panel** has three tabs:
  - **Log**: the detailed log.
  - **Catalog**: command and category counts, plus any validation errors.
  - **System info**: details you can **Copy** into a bug report.

## Keyboard shortcuts

| Keys | Action |
|---|---|
| `Ctrl+K` | Open the Command Palette |
| `Ctrl+F` | Search the current list |
| `Ctrl+H` | Home |
| `Ctrl+B` | Show or hide the navigation pane |
| `Ctrl+,` | Settings |
| `Ctrl+L` | Log viewer |
| `F5` | Check for updates |
| `Ctrl+Q` | Exit |
| `↑` / `↓` | Move the selection (lists and palette) |
| `Enter` | Open the selected command |
| `Esc` | Close a dialog or cancel |

Shortcuts are paused while a dialog is open, and `AltGr` combinations never trigger them, so
typing characters like `@` on international keyboards is safe. The list is also under **More →
Keyboard shortcuts**.

## Updates

The app checks GitHub for new versions on startup (unless you turned that off) and when you press
`F5`.

- When an update is available, choose **Download & restart**.
- Closing the dialog cancels the download.
- The app never restarts while a command is running.

See [UPDATES.md](UPDATES.md) for details.

## Your data and uninstalling

Everything stays on your PC:

- Settings, history, recipes, your commands and profiles are in `%AppData%\CommandForge`.
- Logs are in `%AppData%\CommandForge\logs`.
- The program itself is in `%LocalAppData%\CommandForge`.

**More → Portable info** shows the exact folders. Nothing is sent anywhere except the update check
— see [PRIVACY.md](PRIVACY.md).

To uninstall, go to **Settings → Apps → Installed apps** in Windows (**Apps & features** on
Windows 10), find **CommandForge** and choose **Uninstall**. To also remove your settings and history, delete `%AppData%\CommandForge`
afterwards.

## Troubleshooting

- **SmartScreen or antivirus warnings**: releases aren't code-signed yet. See
  [verifying your download](README.md#unknown-publisher-and-verifying-your-download).
- **Nothing happens when I open the app**: it runs as a single instance, so a second launch brings
  the existing window forward. If it still doesn't appear, check the logs in
  `%AppData%\CommandForge\logs`.
- **An admin command didn't run**: it needs the UAC prompt to be approved. Run it again to get a
  new prompt. If admin commands keep failing, use **End admin session** and try again.
- **Output looks garbled**: please report it with the command and your Windows display language.
- **Something went wrong**: the error dialog has an **Error code** and **Copy details**.
  **Report a bug** (also in the More menu) opens a pre-filled GitHub issue. Nothing is sent until
  you submit it. Export the logs to attach them, but review them first.
