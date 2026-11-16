# Accessibility

CommandForge should be usable by everyone who maintains a Windows PC — with a keyboard alone, with
a screen reader, with a Windows contrast theme or Magnifier, with larger text, and without telling
colours apart. This page says what CommandForge aims for, what works today, how that is checked,
what does not work yet and what to do instead, and how to tell us about a barrier. It is written
for the person who has just met one.

CommandForge is a desktop program: its own window, its dialogs and the notifications it shows.
The commands it runs are Windows' own tools (winget, DISM, PowerShell and others), so the text
they print is theirs.

*Last reviewed on 2026-11-16 for version 2.2.0 by the maintainer, poli0981. In the app: More (⋯) ▸
About CommandForge ▸ Accessibility.*

## What CommandForge aims for

- **WCAG 2.2 at level AA**, applied to desktop software as W3C's
  [WCAG2ICT](https://www.w3.org/TR/wcag2ict-22/) guidance explains. This is a goal that guides the
  work, not a claim: no formal evaluation has been made, and CommandForge does not say it conforms
  to any standard.
- **Every task can be done with the keyboard alone.**
- **A screen reader can read every control and every value**, in the language CommandForge is shown
  in, and hears what changes: a command starting, needing permission, finishing.
- **Text and controls keep their contrast** in the light and the dark theme and under Windows
  contrast themes, and **colour is never the only way something is shown.**
- **Text follows your size settings**: the app's own font size and Windows' Text size.

Accessibility fixes are always in scope, whatever else the project is or isn't taking on.

## What works today

### The keyboard

| Keys | What they do |
|---|---|
| **Tab** and **Shift+Tab** | Move through a page or a dialog in reading order. In a list, Tab moves into the buttons of the selected row (its star, Undo, Run again). |
| The **arrow keys** | Move inside a list: commands, History, the console's output, the Log viewer, the Command Palette's results. On a splitter, they resize the panes. |
| **F6** and **Shift+F6** | Jump between the navigation pane, the page and the console. |
| **Enter** or **Space** | Press the button, switch or box that has the focus. **Enter** on a command in the catalog moves to its **Run** button; on a run in History it opens that command; in the Command Palette it opens the highlighted one. |
| **Esc** | Closes a dialog the way its **Cancel** or **Close** button does: nothing is changed. |
| **Ctrl+K**, **Ctrl+F**, **Ctrl+H**, **Ctrl+,**, **Ctrl+L** | Open the Command Palette, search the current list, Home, Settings, the Log viewer — and move the keyboard into that page. |

Every shortcut is in the user guide, under [Keyboard shortcuts](USAGE.md#keyboard-shortcuts), and in
the app under More (⋯) ▸ Keyboard shortcuts.

- **A risky command can't be run by accident.** A Caution command asks first, with **Cancel**
  focused and as the default; a Dangerous one waits until its name or ID is typed — the ID is read
  out with the box.
- **Ctrl+Q does nothing while you are typing** in a text box (other than a search box), so a slip
  can't close the app in the middle of an edit.

### Screen readers

- **Every button, switch, box and list item has a name**, from the same translations as the screen.
  A button repeated on every row names its row — *Undo: Flush DNS cache*, *Move up: Clean temporary
  files*, *Add to favorites: Check disk*.
- **A command is read with its risk**: *Reset Windows Update, Caution, Requires admin, May require
  restart* — what a sighted user reads from the coloured dot and the badges.
- **Every page has a heading**: its title is a heading of the first level, each section one of the
  second, so a screen reader's heading keys move from section to section.
- **What appears is said as it appears**: a command starting, waiting for administrator permission,
  finishing (with its result, exit code and time, and whether Windows must restart), how many
  commands a search found, the highlighted result in the Command Palette (*Flush DNS cache, Safe, 2
  of 14*), every notification at the bottom of the window, and an update's progress.
- **The console and the Log viewer can be read line by line**, and an error line starts with
  *Error*.
- **Icons are left out**: they only decorate the word beside them, so a screen reader never stops on
  a symbol. A notice on a page is read in full with its kind first — *Warning: These commands are
  not part of the vetted catalog…*
- **Dialogs say they are dialogs**, keep the focus inside them until they close, and give it back.

### Seeing the screen

- **Light, dark, or the same as Windows** (Settings ▸ Appearance ▸ Theme). Every colour comes from
  the theme.
- **Windows contrast themes** (Settings ▸ Accessibility ▸ Contrast themes) always win over the app's
  own theme, as soon as you turn one on or off. Dimmed text and badge icons are drawn at full
  strength under a contrast theme.
- **Text size**: the app's own font size (Small, Medium, Large) is multiplied by Windows' **Text
  size** (Settings ▸ Accessibility ▸ Text size, up to 225 %), picked up when you come back to the
  app; at the largest sizes the navigation pane widens to fit its labels. **Display scaling** works
  at any size, on every screen, and so does **Magnifier** (Windows logo key + Plus).
- **Colour is never the only signal**: risk levels always come with their word or an icon, and an
  error line in the console has a bar at its left as well as its colour.
- **Animations follow Windows' Animation effects setting**: with it off, pages switch at once.

### Languages

CommandForge is shown in English, Vietnamese, Japanese, Simplified Chinese or Spanish (Settings ▸
Appearance ▸ Language), and the names a screen reader reads come from the same translations. The
documents — the user guide, this page, the legal texts — are in English.

## How it is checked

- **On every build**, by the project's tests: every control that shows no text has a name; the
  items of a list are named by what they show, not by a program type; every page title is a
  heading; the app declares that it follows each screen's scaling; no colour is written into a
  screen; every icon is kept out of a screen reader's way; this page's links work and it never
  claims conformance. The key every name uses must exist in all five languages. Other tests run the
  fixes on real controls of the WPF-UI library the app is built with, so a library update that
  renames one of their parts is caught before a release.
- **By an automated scan**: Axe.Windows, Microsoft's UI Automation rules (the engine of Accessibility
  Insights for Windows), over every page of the running app, the first-start terms and the console
  after a command, in the light and the dark theme and at Windows' largest Text size. Version 2.2.0
  passes it with no error.
- **By hand**: the maintainer used CommandForge 2.1.0 with the keyboard alone (mouse disconnected)
  and with Narrator on Windows 11 in November 2026 and found no major problem. Version 2.2.0 then
  fixed everything a review of every screen found — the result of a command was not announced, the
  console could not be read line by line, the Command Palette's highlight was silent, and some lists
  read a program type instead of their text, among others.
- **Not tested yet**: NVDA, JAWS, Voice Access, touch, and Windows 10. They should work through the
  same Windows interface Narrator uses, but nobody has checked.

## Known limitations

Each one with what to do instead. A barrier that is not on this list is worth reporting.

- **A command's output is the text Windows' tool prints**, in Windows' language, sometimes with
  progress bars drawn out of characters. Read it line by line, or use **Copy all** or **Save** above
  the console to read it elsewhere.
- **There are no access keys** (Alt with an underlined letter): with five languages they would clash.
  Use the shortcuts above, Tab and F6.
- **The title bar's Minimize, Maximize and Close buttons are not Tab stops** (screen readers still
  find them by name). Use **Alt+F4** or **Ctrl+Q** to close, and Windows' own **Windows logo key +
  arrow** keys to maximize, restore or minimize.
- **Expanding a card in Settings still animates** when Windows' Animation effects is off: the
  animation belongs to the WPF-UI library the app is built with.
- **At the largest sizes** (Large font with Windows' Text size near 225 %), some pages and dialogs
  need scrolling, and the title bar's search box shows the start of its text (its tooltip and its
  name have all of it; Ctrl+K opens it).
- **Icons keep their size**: Windows' Text size enlarges text only. Display scaling (Settings ▸
  System ▸ Display ▸ Scale) or Magnifier enlarges everything.
- **The windows Windows itself shows** — the administrator (UAC) prompt, choosing a file — are
  Windows', not CommandForge's.

## Report an accessibility problem

1. Open an issue with the
   [Accessibility problem form](https://github.com/poli0981/CommandForge/issues/new?template=accessibility.yml),
   or write to **[accessibility@poli0981.dev](mailto:accessibility@poli0981.dev)** if you'd rather not
   use GitHub or not post in public.
2. Say what you were trying to do and what happened instead; your CommandForge version (More ▸ About)
   and your Windows version; and what you use — the keyboard alone, Narrator, NVDA, JAWS, Magnifier,
   a contrast theme, a larger text size — with its version. The steps to get there help, and so does
   a screenshot or a recording, but neither is required. **You never need to tell us anything about
   yourself or a disability.**
3. If you want to attach details from your PC, **More ▸ Report a bug** gathers them and exports the
   log as a file you review first; nothing is uploaded by itself.

**A safety prompt you could not perceive or answer** — a Caution or Dangerous confirmation — is a
safety problem as well: say so in the report, and it is handled first.

### How serious it is

You do not have to choose; the maintainer sets it when reading the report.

| Severity | What it means | For example |
|---|---|---|
| **Critical** | A task cannot be done at all. | A Dangerous command's confirmation cannot be answered with the keyboard; the terms cannot be accepted on first start; a command's result is never conveyed. |
| **Serious** | It can be done only with great difficulty or a workaround. | A command's output can be read only after saving it to a file. |
| **Moderate** | It is tiring or inconsistent. | A name that differs from what the screen shows; an extra stop for Tab. |
| **Minor** | It barely gets in the way. | A tooltip the keyboard cannot reach, when its words are shown elsewhere too. |

### What happens next

CommandForge has one maintainer, so there are no promised times. Every report is read and answered,
without doubting what you describe. Critical problems come before other work; a workaround, when
there is one, is written on the issue; the fix ships in a release, and the issue is closed once you
say it works for you — or when a fix has been out for a while and nobody has answered.

## Who looks after this

The maintainer, poli0981, keeps this page and the checks behind it, and reviews both for each release
that changes a screen. A change to CommandForge that moves anything on this page changes the page in
the same pull request.

## For contributors

A change to the app keeps what the tests above hold: a name, from the translations in all five
languages, for every control that shows no text and every list item; colours only from the theme.
Say in the pull request what you checked with the keyboard and with Narrator (Accessibility Insights
for Windows is a good companion). The rules are in [CONTRIBUTING.md](.github/CONTRIBUTING.md).

## Feedback

To improve this page, open an issue or a pull request. A barrier goes through the form above, so that
it is not lost.
