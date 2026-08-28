---
title: Fix Failing Tests One Package at a Time
impact: MEDIUM
impactDescription: One compile error in a package fails every suite in it — and everything downstream
tags: testing, debugging, workflow, swiftpm, xcodebuild
---

## Fix Failing Tests One Package at a Time

**Impact: MEDIUM**

A red run in a Swift project is usually **not** thirty broken behaviours. It is one compile error —
in the package under test, or in a package it depends on. Everything downstream fails to build, and
the runner reports it as N test failures. Chasing them all at once wastes the session; walking the
dependency graph from the bottom takes minutes.

**The order, always — bottom of the dependency graph upwards:**

1. **Build the lowest package that changed.** `Platform` → `DesignSystem` → `Money` → `ApiClient` →
   `WalletStore` → features → app. A failure at the bottom explains every failure above it.
2. **Build its test target** (`swift build --build-tests`). If the tests do not compile, nothing
   below is real signal.
3. **Run one suite** with `--filter`.
4. Get that package fully green before moving up a level.
5. Only when the packages are green, generate the project and run the app-level tests.

```sh
cd ios

# 1 + 2 — compile the package and its tests, without running them
swift build --build-tests --package-path Packages/Money

# 3 — a single suite
swift test --package-path Packages/Money --filter FXRateTests
swift test --package-path Packages/Money --filter "FXRateTests/roundsUpAfterMargin"

# 4 — the whole package, then the one directly above it
swift test --package-path Packages/Money
swift test --package-path Packages/WalletStore

# 5 — app level, last (composition root, ios/Tests/)
xcodegen generate
xcodebuild test -project MoniPay.xcodeproj -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro,OS=26.4' \
  -only-testing:MoniPayTests
```

`swift test` output is already readable. `xcodebuild` is not — filter it:

```sh
xcodebuild test ... 2>&1 | grep -E "error:|✘|Test .* failed" | sort -u | head -30
```

**Incorrect (batch-fixing symptoms while the real cause sits one package down):**

```text
TopUp, Cards and Home suites are red → open all three and guess →
`xcodebuild test` on the app (4 min) → still red, plus three half-edits to unwind.
```

**Correct (walk down the graph, fix once):**

```text
TopUp, Cards, Home red → all three depend on WalletStore → walk down:
  swift test --package-path Packages/WalletStore
  Sources/WalletStoreTestSupport/StubTopUpService.swift:9: error: type 'StubTopUpService'
  does not conform to protocol 'TopUpService'      (the port changed)
→ fix the double in TestSupport → swift test WalletStore green →
  TopUp, Cards and Home go green without being touched.
```

**Four Swift-specific traps:**
- **Parallel suites.** A test that passes with `--filter` but fails in the full run is sharing
  mutable state (a `static var`, a singleton `Store`, a mutated `Locale`/`TimeZone` default). Fix
  the sharing — do not reach for `.serialized` to hide it.
- **A downstream package still red after an upstream fix** → `rm -rf Packages/<Name>/.build` and
  re-run; SwiftPM occasionally keeps a stale module for a renamed type.
- **`no such module 'Money'` at the app level only** → the package is missing from `packages:` or
  from the target `dependencies:` in `ios/project.yml`. `swift test` cannot catch this;
  `xcodegen generate` + `xcodebuild` can.
- **Stale generated project** after editing `project.yml`: `xcodegen generate` before concluding
  anything about a bizarre app-level failure.

**Never** delete, `.disabled(...)`, or `withKnownIssue { }` a failing test to get a green run. If a
test must be parked, it carries `.bug("<issue url>")` and the reason, in its own commit.

Reference: [Swift Package Manager — swift test](https://www.swift.org/documentation/package-manager/)
