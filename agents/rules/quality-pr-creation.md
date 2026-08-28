---
title: PR Creation Best Practices
impact: HIGH
impactDescription: PRs that skip the local gates burn a full review cycle
tags: pull-request, code-review, workflow, xcodebuild, dotnet
---

## PR Creation Best Practices

**Impact: HIGH**

### Draft by default

Open pull requests as drafts (`gh pr create --draft`). A human marks a PR ready for review when it actually
is — not when the agent stops typing.

### Title: Conventional Commits

Squash merge makes the PR title the commit message on `main`, and `.github/workflows/pr-title.yml`
validates it. Match that workflow exactly.

- Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.
- **Scope is the component, not the package**: `api`, `ios`, `mobile`, `build`, `ci`, `docs`, `release` —
  the list in `pr-title.yml`. Name the package in the subject instead.
- Be specific: `fix(ios): reject top-ups below the Campay minimum in live mode`,
  `feat(api): expose the wallet authorization endpoint`.
- Not generic: `fix: top-up bug`.
- Imperative mood, English, no trailing period, subject ≤ 72 characters. Commit messages and PR
  descriptions are English too — see `quality-english-only-code.md`.

### Scope: one component per PR

A pull request touches **either `ios/` or `server/`**, not both. They have separate toolchains, separate CI
workflows (`ci.yml` vs. `monipay-api-ci.yml`), and separate reviewers; a mixed PR fails one gate for
reasons unrelated to the other half.

The single exception is a change to the **OpenAPI contract**. When the server's HTTP surface changes, the
server change and the regenerated `ios/Packages/ApiClient` DTOs ship in the same PR — otherwise `main` has
a client and a server that disagree. Say so in the description, and keep everything else out of that PR.

Changes to `agents/`, `docs/`, or a workflow are their own PR (`docs:`, `ci:`, `chore:`).

### Size limits

- Over ~500 changed lines or ~10 files is too large for a useful review.
- Split along the package graph, bottom-up: `Money` model change → `ApiClient`/`WalletStore` wiring →
  the feature package's views → `ios/App/` composition → tests.
- Split by package: a change in `Packages/Cards` and one in `Packages/TopUp` are two PRs. Feature packages
  never depend on each other, so they almost never need to ship together.
- A new package must ship with its `Package.swift`, its entry in `ios/project.yml`, and the dependency edge
  in every consuming `Package.swift`.
- `MoniPay.xcodeproj` is generated and gitignored; it must never appear in a diff.

### Before pushing — iOS

```bash
cd ios

# 1. Fastest loop: build and test only the packages you touched.
swift build --package-path Packages/Money
swift test  --package-path Packages/Money

# 2. Regenerate the project if ios/project.yml, a Package.swift, or the file tree changed.
xcodegen generate

# 3. Lint, exactly as CI runs it (config is ios/.swiftlint.yml).
swiftlint lint --strict --quiet

# 4. Full build + tests against the iOS 26 simulator.
xcodebuild -project MoniPay.xcodeproj -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17,OS=26.0' \
  build test | xcbeautify
```

### Before pushing — backend

```bash
export HUSKY=0                     # CI sets this; git hooks must not run in an agent session

dotnet restore server/MoniPay.slnx
dotnet build   server/MoniPay.slnx --no-restore --configuration Release -warnaserror
dotnet format  server/MoniPay.slnx --no-restore --verify-no-changes
dotnet test    server/MoniPay.slnx --no-build --configuration Release
```

`dotnet format --verify-no-changes` fails instead of rewriting files, which is what CI runs — run
`dotnet format server/MoniPay.slnx` without the flag to fix, then re-verify. Warnings are errors: do not
suppress one to get green without a `#pragma warning` justification in the diff.

A green build is a claim you must be able to back with the command output. Do not report "builds fine"
from reading the diff.

### Description template

```markdown
## What
One paragraph: what changes for the user.

## Why
The problem or the ticket. Link it.

## How
Key implementation decisions and anything a reviewer would otherwise have to reverse-engineer
(money rounding, concurrency isolation, a provider workaround).

## Verified
iOS
- [ ] `swiftlint lint --strict` clean (run from `ios/`)
- [ ] `swift test` green for every package touched
- [ ] `xcodebuild build test` green on iPhone 17 / iOS 26
- [ ] New UI copy in the owning package's `Localizable.xcstrings`, English key + `fr` translation,
      read with `bundle: .module`
- [ ] No new feature-to-feature dependency; package graph direction respected
- [ ] No `!`, `try!`, `as!` introduced
- [ ] Tested on a device or simulator; screenshots below if the UI changed

Backend
- [ ] `dotnet build -warnaserror` clean
- [ ] `dotnet format --verify-no-changes` clean
- [ ] `dotnet test` green

Both
- [ ] No French in code (identifiers, comments, test names, log messages)
- [ ] One component only (`ios/` **or** `server/`), unless this is an OpenAPI contract change shipping
      with the regenerated `ApiClient`

## Screenshots
Before / after for any UI change, light and dark, Dynamic Type XXL if layout changed.
```

### Never commit

Secrets (Campay / Sudo Africa keys — they belong in `ios/Config/*.xcconfig`, which stays out of git),
`.env` files, `xcuserdata/`, `Packages/*/.build/`, or the generated `MoniPay.xcodeproj` (edit
`ios/project.yml` instead).

Reference: [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)
