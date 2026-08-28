---
title: Git Workflow — Branches, Conventional Commits, and a Generated Project
impact: HIGH
impactDescription: The xcodeproj is gitignored; what must be reviewed is project.yml and Package.swift
tags: git, ci, workflow, xcodegen, swiftpm, commits
---

## Git Workflow — Branches, Conventional Commits, and a Generated Project

**Impact: HIGH**

**Never commit directly to `main`, never force-push `main`.** Every change lands through a branch,
even a one-line fix.

**Branch naming** — `<type>/<short-kebab-subject>`, English:

```
feat/topup-campay-client
fix/wallet-hold-released-twice
refactor/split-cards-package
chore/bump-deployment-target-ios26
docs/agents-rules
```

**Conventional commits**, subject in the imperative, ≤ 72 characters. Scope is the **package name**,
which makes the history readable as a module log. Body explains *why*.

```
feat(TopUp): wire the MoMo flow to the Campay POC

The amount is capped at 25 XAF in the demo sandbox (Campay limit). The FCFA
debit stays server-side: the client only transports.

fix(WalletStore): stop releasing an expired hold twice
test(Money): cover the round-up in FXRate.xaf(fromUSDCents:)
chore(ios): declare the Convert package in project.yml
```

Allowed types: `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `chore`, `build`, `ci`.

**Rebase, never merge, to catch up with `main`:**

```sh
git fetch origin
git rebase origin/main          # linear history
git push --force-with-lease     # never bare --force, never on main
```

`--force-with-lease` refuses the push if someone else advanced your branch; plain `--force`
destroys their work silently.

### The Xcode project is generated and gitignored

`ios/MoniPay.xcodeproj` is produced by XcodeGen from `ios/project.yml` and is **in `.gitignore`**.
It is never staged, never reviewed, and **`project.pbxproj` merge conflicts cannot happen** — the
class of conflict that used to eat afternoons is designed out. What replaces it is discipline about
the real source files.

**Incorrect (making a structural change through Xcode's UI):**

```text
Xcode → File → New → Target… / drag a folder into the navigator
→ the .xcodeproj changes… and it is gitignored, so nothing is committed.
→ on a teammate's machine, `xcodegen generate` rebuilds the project WITHOUT the change.
→ "works on my machine" for two days.
```

**Correct (structure lives in text files, reviewed like code):**

```sh
# New target, new setting, new package → ios/project.yml
$EDITOR ios/project.yml          # packages: <Name> + dependencies: - package: <Name>

# Sources, internal dependencies, products of a package → its Package.swift
$EDITOR ios/Packages/Convert/Package.swift

cd ios && xcodegen generate      # local only, never committed
git add ios/project.yml ios/Packages/Convert
```

The reviewable diff is `project.yml` + `Package.swift`. If a PR changes app structure without
touching either, the change is invisible to everyone else.

### `Package.resolved`

- A package with **only local path dependencies** has no `Package.resolved`. Normal — do not create
  one.
- A package that gains a **remote** dependency gets `ios/Packages/<Name>/Package.resolved`, and that
  file **is committed**: it pins the exact revision CI and every teammate build resolves.
- Never gitignore it, and never hand-edit it. To change a pin, edit `Package.swift` and run
  `swift package update --package-path ios/Packages/<Name>`, then commit the regenerated file.
- On a `Package.resolved` conflict, do not merge the JSON: take either side, re-run
  `swift package resolve --package-path ios/Packages/<Name>`, and commit the result.
- `.build/`, `.swiftpm/`, `DerivedData/` and the generated `Config/MoniPay-Info.plist` stay ignored.

### Before every push

1. `cd ios` and test each package you touched: `swift test --package-path Packages/<Name>` for `Platform` / `ApiClient`,
   `(cd Packages/<Name> && xcodebuild -scheme <Name> -destination '…' test)` for any package that imports `DesignSystem`
2. `swiftlint --strict`
3. `xcodegen generate && xcodebuild build …` if `App/`, `Config/` or `project.yml` moved
   (see `ci-build-first`)
4. `git status` — no `.env`, no `.xcodeproj`, no `.build/`, no `xcresult` bundle staged.
   **`poc/.env` holds live Campay and Sudo sandbox credentials and must never be committed.**

Push, *then* watch CI: it runs on the remote state, so waiting on an unpushed commit waits forever.

Reference: [Conventional Commits](https://www.conventionalcommits.org/) ·
[XcodeGen project spec](https://github.com/yonaskolb/XcodeGen/blob/master/Docs/ProjectSpec.md)
