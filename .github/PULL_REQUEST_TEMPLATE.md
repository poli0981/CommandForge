<!--
Keep PRs focused and small. Read CONTRIBUTING.md before submitting.
Do NOT delete tests or use --no-verify to bypass hooks.
-->

## Summary

<!-- What does this PR change, and why? -->

## Related issue

<!-- e.g. Closes #123 -->

## Checklist

- [ ] `dotnet build -c Release` passes with **0 warnings**
- [ ] `dotnet test` passes (and I added/updated tests for new behavior)
- [ ] `dotnet format --verify-no-changes` passes; no unrelated reformatting
- [ ] No tests deleted/disabled; hooks not bypassed (`--no-verify` not used)
- [ ] My commits are signed (`main` requires verified signatures)
- [ ] New or changed UI text is in all five `Strings*.resx` files (EN, VI, JA, zh-Hans, ES) — no hardcoded strings
- [ ] If I added a command: it's a vetted catalog entry with correct `requiresAdmin`/`dangerLevel`, strings in all five languages, and no arbitrary-execution path
- [ ] **AI disclosure:** I have disclosed any AI assistance used (delete if none)
