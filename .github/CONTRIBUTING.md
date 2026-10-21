# Contributing to CommandForge

Thanks for your interest! CommandForge is a GPLv3, offline-first Windows tool that runs
system-level commands, so **safety and clarity matter more than speed**. Please read this before
opening an issue or pull request.

## Quick start

Requires Windows 10 22H2 / 11 and the .NET 10 SDK.

```bash
dotnet restore
dotnet build -c Release                  # must be 0 warnings (warnings are errors)
dotnet test                              # all tests must pass
dotnet format --verify-no-changes        # match the code style (run `dotnet format` to fix)
```

See [BUILD.md](../BUILD.md) for packaging and CI.

## Reporting issues

- Use the issue templates (Bug report / Feature request).
- **Get straight to the point**: what you expected, what happened, exact steps, your Windows
  version/build and the app version (**More → About** in the app).
- For **security vulnerabilities, do NOT open a public issue** — see [SECURITY.md](SECURITY.md).
- Questions and ideas: [Discussions](https://github.com/poli0981/CommandForge/discussions), or
  email [code@poli0981.dev](mailto:code@poli0981.dev).

## Pull requests

- Keep PRs **focused and small**; one logical change per PR.
- Match existing style; run `dotnet format` and keep the build at **0 warnings** (analyzers,
  including SonarAnalyzer, run as part of the build).
- **Don't delete or disable tests** to make CI pass; add tests for new behavior.
- **Sign your commits** (GPG, SSH or S/MIME) — `main` only accepts verified signatures. Commit
  messages follow [Conventional Commits](https://www.conventionalcommits.org/)
  (`fix(catalog): …`, `feat(ui): …`).
- PRs need the required checks to pass: `build`, `package`, CodeQL (`Analyze (csharp)`,
  `Analyze (actions)`) and `dependency-review`.
- **Dependencies:** package versions live in each `.csproj` (GitHub's dependency graph doesn't read
  `Directory.Packages.props`). Discuss a new package before adding it; runtime dependencies must be
  GPLv3-compatible (dependency review enforces a license allow-list).
- **UI text** goes in the `.resx` files, never hardcoded, and every key needs a value in all five
  languages: `Strings.resx` (English), `Strings.vi.resx`, `Strings.ja.resx`, `Strings.zh-Hans.resx`
  and `Strings.es.resx`. A test fails when a key the UI uses is missing from any of them.
  Machine-translated text is fine if you say so; native-speaker fixes are very welcome.
- **Disclose AI assistance.** AI-assisted contributions are welcome — just say so in the PR
  description.

### Adding a command

Commands live only in the embedded catalog,
`src/CommandForge.Infrastructure/Catalog/commands.json`. Never add a code path that runs arbitrary
commands or elevates outside the vetted catalog.

- Give it a unique `id`, a `categoryId` from `categories.json`, and `titleKey`/`descriptionKey`
  strings in all five languages.
- Keep `requiresAdmin` and `dangerLevel` accurate:
  - **Safe** — no risk of losing data: read-only queries, opening a tool or settings page, or a
    small preference change that is undone the same way it was made.
  - **Caution** — changes system state (services, registry, caches, boot-time checks);
    recoverable, but undoing it may take effort.
  - **Dangerous** — may lose data, remove a rollback option or leave Windows unstable.

  Caution and Dangerous commands need `"confirmBeforeRun": true`.
- `executable` is a plain program name (it is resolved to a trusted absolute path). PowerShell
  commands must pass `-NoProfile -NonInteractive`.
- Use the `{SystemDrive}`, `{SystemRoot}` and `{UserProfile}` placeholders instead of hard-coded
  paths or `%VARIABLES%`.
- Use `minOsBuild` / `maxOsBuild` for commands that only exist on some Windows builds, and
  `revertCommandId` when another catalog command undoes this one.
- The `icon` name must be mapped in `src/CommandForge.Wpf/Converters/IconMap.cs`.

The catalog tests check these rules, and the app's loader rejects invalid records.

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
use the project or its spaces to attack, insult, or harass others. Report problems to
[code@poli0981.dev](mailto:code@poli0981.dev).

## License

By contributing, you agree your contributions are licensed under the project's
[GPLv3](../LICENSE).
