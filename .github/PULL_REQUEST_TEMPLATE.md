<!--
Keep PRs focused and small. Read CONTRIBUTING.md before submitting.
Do NOT delete tests or use --no-verify to bypass hooks.
-->

## Summary

<!-- What does this PR change, and why? -->

## Related issue

<!-- e.g. Closes #123 -->

## Scope

<!-- CommandForge is in maintenance mode: .github/CONTRIBUTING.md#maintenance-mode-what-we-accept -->

- [ ] This is within maintenance mode: a dependency or security update, a platform update, a catalog command added, fixed or removed, a bug, UI/UX or accessibility fix, or a translation fix — or a new feature agreed in an issue as genuinely necessary

## Checklist

- [ ] `dotnet build -c Release` passes with **0 warnings**
- [ ] `dotnet test` passes (and I added/updated tests for new behavior)
- [ ] `dotnet format --verify-no-changes` passes; no unrelated reformatting
- [ ] No tests deleted/disabled; hooks not bypassed (`--no-verify` not used)
- [ ] My commits are signed (`main` requires verified signatures)
- [ ] New or changed UI text is in all five `Strings*.resx` files (EN, VI, JA, zh-Hans, ES) — no hardcoded strings
- [ ] If I added a command: it's a vetted catalog entry with correct `requiresAdmin`/`dangerLevel`, strings in all five languages, and no arbitrary-execution path
- [ ] If I changed a screen: every control without text has an `AutomationProperties.Name` from the `.resx`, list items are named by what they show, icons are `ctl:DecorativeIcon`, and I tried it with the keyboard alone (and Narrator if I could) — see [ACCESSIBILITY.md](../ACCESSIBILITY.md#for-contributors)
- [ ] **AI disclosure:** I have disclosed any AI assistance used (delete if none)
