---
title: SwiftLint Must Pass Before You Claim Done
impact: CRITICAL
impactDescription: A clean lint run is the cheapest quality gate we have
tags: quality, swiftlint, ci, tooling, packages
---

## SwiftLint Must Pass Before You Claim Done

**Impact: CRITICAL**

SwiftLint is installed at `/opt/homebrew/bin/swiftlint` and configured by **`ios/.swiftlint.yml`**, whose
`included:` covers the whole iOS tree:

```yaml
included:
  - App
  - Tests
  - Packages/*/Sources
  - Packages/*/Tests
excluded:
  - Packages/*/.build
```

Lint is not advisory. A PR with violations is not ready for review, and an agent that says "done" without
having run it has not finished the task.

**Run it from `ios/`, before every commit:**

```bash
cd ios && swiftlint lint --strict --quiet
```

`--strict` promotes warnings to errors, which is how CI runs it. Exit code 0 or you are not done.
`swiftlint --fix` applies the autocorrectable rules (spacing, trailing whitespace, redundant type
annotations) — run it first, re-lint, then re-read the diff it produced.

Because the config lives at `ios/.swiftlint.yml` and the paths are relative to it, running the binary from
the repo root or from inside a single package lints the wrong set of files. Always run it from `ios/`.

**The rules that matter most here** (configured as errors): `force_unwrapping`, `force_try`, `force_cast`,
`implicitly_unwrapped_optional`, `todo` (unowned TODOs), `line_length`, `file_length`, `type_body_length`,
`cyclomatic_complexity`, `large_tuple`.

**Never disable a rule to make the build green.** `// swiftlint:disable` is forbidden by
`quality-zero-warnings.md` — including `disable:next`, and including a new `excluded:` entry or a raised
threshold in `ios/.swiftlint.yml`. The single escape hatch is a provable upstream false positive, and it
carries four conditions (narrowest scope, a linked upstream issue, an owned `TODO`, its own PR). Read that
rule before you type the word `disable`.

**Incorrect (blanket suppression, no reason, hides three real problems):**

```swift
// swiftlint:disable force_unwrapping force_try line_length
// (file-wide, added to make CI pass)

let card = store.cards.first!
let data = try! JSONEncoder().encode(payload)
```

**Correct (fix the code — that is the whole list of acceptable responses):**

```swift
guard let card = store.cards.first else { return }
let data = try JSONEncoder().encode(payload)   // the enclosing function throws
```

The rare documented exception looks like the example in `quality-zero-warnings.md`: one line, one rule, a
link to the upstream issue, an owned `TODO`, and a PR of its own.

**Adding a rule** is a change to `ios/.swiftlint.yml` in its own commit, with the reason in the commit
message; it applies to every package at once, so say so in the PR description. **Relaxing** one — raising a
threshold, widening `excluded:`, moving a rule to `disabled_rules:` — is a suppression under a different
name and is governed by `quality-zero-warnings.md`. Never do it in the same PR as the code that would
otherwise fail.

**Formatting.** `swiftformat` is not installed in this environment. SwiftLint's autocorrect plus Xcode's own
indentation (Ctrl-I) is the formatting baseline: 4-space indent, no trailing whitespace, one blank line
between top-level declarations, and a trailing newline at end of file.

Reference: [SwiftLint rule directory](https://realm.github.io/SwiftLint/rule-directory.html)
