# Contributing to CommandForge

Thanks for your interest! CommandForge is a GPLv3, offline-first Windows tool that runs
system-level commands, so **safety and clarity matter more than speed**. Please read this before
opening an issue or pull request.

## Quick start

```bash
dotnet restore
dotnet build -c Release   # must be 0 warnings (warnings are errors)
dotnet test               # all tests must pass
dotnet format             # match the code style
```

See [BUILD.md](../BUILD.md) for publish/packaging.

## Reporting issues

- Use the issue templates (Bug report / Feature request).
- **Get straight to the point**: what you expected, what happened, exact steps, your Windows
  version/build and the app version (Settings → About).
- For **security vulnerabilities, do NOT open a public issue** — see [SECURITY.md](SECURITY.md).

## Pull requests

- Keep PRs **focused and small**; one logical change per PR.
- Match existing style; run `dotnet format` and keep the build at **0 warnings**.
- **Don't delete or disable tests** to make CI pass; add tests for new behavior.
- **Adding a command?** Add it to the catalog JSON (`Infrastructure/Catalog/commands.json`) per
  the schema, add the EN/VI resource strings, and keep `requiresAdmin`/`dangerLevel` accurate.
  Never add a code path that runs arbitrary commands or self-elevates outside the vetted catalog.
- **Disclose AI assistance.** AI-assisted contributions are welcome — just say so in the PR
  description.

## AI-assisted contributions

AI tools are welcome for writing and reviewing changes **when disclosed**. What is *not*
welcome is an unsolicited "AI rewrote your whole codebase in a different style" PR — those are
closed.

## Auto-ignored / rejected contributions

To protect users, the following are **closed without review** and may result in a contributor
ban — no exceptions:

- **Suspected malicious code.** If a PR appears to contain a supply-chain attack, obfuscation,
  credential harvesting, a cryptominer, or any malware — even if CodeQL and Dependabot don't flag
  it — the maintainer may investigate manually. **Detection equals a ban.**
- **Unverified or suspicious links** in issues, PRs, or comments (pastebin scrapers, redirect
  chains, unfamiliar shortlinks, link-shortener URLs without context).
- **Behavior or language violating**
  [GitHub's Acceptable Use Policies](https://docs.github.com/site-policy/acceptable-use-policies).
- **Off-topic / rambling reports** that don't get to the point (e.g. *"Good morning, today is a
  beautiful day, I was just wondering if maybe…"*). Get straight to the problem.
- **PRs that touch hundreds of files for a one-line fix** (a typical sign of an auto-formatter run
  on unrelated files).
- **Deleted tests, or `--no-verify` used to bypass pre-commit hooks.**
- **"AI rewrote my entire codebase in a different style" PRs.** AI-assisted edits are welcome
  (disclosed); whole-repo rewrites are not.
- **Spam, drive-by typo fixes** that don't address a real issue, and karma-farming PRs.

## Conduct

By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md). Be respectful; do not
use the project or its spaces to attack, insult, or harass others.

## License

By contributing, you agree your contributions are licensed under the project's
[GPLv3](../LICENSE).
