---
title: Triage a Red CI by Reproducing It Locally, Exactly
impact: HIGH
impactDescription: Guessing at CI failures burns whole sessions on the wrong file
tags: ci, debugging, swiftpm, xcodebuild, swiftlint
---

## Triage a Red CI by Reproducing It Locally, Exactly

**Impact: HIGH**

Never fix a CI failure by pushing a speculative change and waiting for the next run. The loop is
ten minutes long and teaches you nothing. Copy **the exact command the failing job ran**, run it
locally, and work from the real error.

**Which tier failed tells you which command to copy.** CI has two:
- **`package-tests`** — `swift test --package-path ios/Packages/<Name>`, one job per package, in
  parallel. Reproducing it needs no Xcode project and no simulator.
- **`full-build`** — `xcodegen generate` then `xcodebuild build` on a **freshly generated** project.
  A failure here that does not reproduce from a package job is almost always a composition-root or
  `project.yml` problem.

**The triage order:**

1. **Read the failure class.** Four buckets, four different responses:
   - `error:` from the Swift compiler → a real type or concurrency error. Fix the code.
   - `error: Unable to find a destination matching …` → simulator/runtime mismatch on the runner,
     **not** your change.
   - `✘ Test "…" recorded an issue` → a real behavioural failure.
   - SwiftLint `Serious violation` → style/complexity gate.
2. **Reproduce the failing tier's command verbatim**, including the destination string — a
   different simulator OS is a different test run.
3. **Compare against `main`.** If `main` is red for the same reason, your branch is not the cause.
4. Only then edit.

```sh
cd ios

# package-tests tier: exactly the job that went red.
swift test --package-path Packages/Money 2>&1 | tee /tmp/branch-money.log

# full-build tier: CI starts from a regenerated project, so do we.
xcodegen generate
xcodebuild build -project MoniPay.xcodeproj -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro,OS=26.4' \
  2>&1 | tee /tmp/branch-build.log | grep -E "error:|warning:" | sort -u

# Lint: config is ios/.swiftlint.yml; a `serious` violation fails the job.
swiftlint --strict --reporter emoji
```

**Confirm it is yours, not the trunk's:**

```sh
git stash push --include-untracked
git checkout main
swift test --package-path ios/Packages/Money 2>&1 | grep -E "error:|✘" | sort -u > /tmp/main.log
git checkout - && git stash pop
diff /tmp/main.log <(grep -E "error:|✘" /tmp/branch-money.log | sort -u)
```

**Incorrect (blind push loop):**

```text
CI red → "must be the simulator" → push a "retry CI" commit → 10 min → red →
change a line at random → push → 10 min → red → 4 noise commits in the history,
and the cause (a test double that no longer conforms to its protocol) still there.
```

**Correct (reproduce, isolate, one fix):**

```text
The "package tests (WalletStore)" job is red → copy its command →
  swift test --package-path ios/Packages/WalletStore
  Sources/WalletStoreTestSupport/StubTopUpService.swift:9: error: type
  'StubTopUpService' does not conform to protocol 'TopUpService'  (the port changed)
→ one fix, one commit, one push, green.
```

**Known non-signals — do not chase these:**
- `Unable to find a destination matching the provided destination specifier` when the runner image
  lacks the iOS 26.4 runtime. Fix the runner image or the workflow's destination, not the code.
- A first-run simulator boot timeout on a cold runner. Re-run the job once; if it repeats, it is
  real.
- **`no such module 'Money'` in the `full-build` tier only** — the package is missing from
  `packages:` or from the `MoniPay` target's `dependencies:` in `ios/project.yml`. The package jobs
  cannot catch this; only a regenerated project can.
- `Signing for "MoniPay" requires a development team` on a locally stale project → `cd ios &&
  xcodegen generate`. The `.xcodeproj` is gitignored, so yours can be arbitrarily out of date.

**Flaky ≠ ignorable.** A test that fails only in the full parallel run is not flaky; it is sharing
state. Fix it as described in `testing-incremental`.

Reference: [Interpreting test results in Xcode](https://developer.apple.com/documentation/xcode/running-tests-and-interpreting-results)
