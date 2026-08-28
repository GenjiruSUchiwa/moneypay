---
title: Leverage AI — But Verify With the Toolchain Before Claiming Done
impact: MEDIUM
impactDescription: Unverified AI output costs more review time than it saves
tags: culture, ai, agents, verification, xcodebuild
---

## Leverage AI — But Verify With the Toolchain Before Claiming Done

**Impact: MEDIUM**

Agents should generate the bulk of the mechanical work in this repo: `Codable` DTOs for the Campay and Sudo
Africa payloads in `ApiClient`, `#Preview` variants, Swift Testing suites inside each package,
`Localizable.xcstrings` entries, boilerplate `View` scaffolding, repetitive refactors. That frees humans for the parts where being wrong is expensive.

**Where AI carries the load:**
- DTOs, mappers, and decoding boilerplate.
- Comprehensive test suites, especially edge cases around money rounding and authorisation.
- Preview providers across light/dark, Dynamic Type, and empty/error states.
- Mechanical refactors: renames, extracting a component, migrating a call site.
- First-pass code review against `agents/rules/`.

**Where a human must own the decision:**
- Money arithmetic, rounding direction, and FX margin.
- Actor isolation and the `Wallet` authorisation state machine.
- Anything touching KYC data, PAN/CVV handling, or provider credentials.
- Navigation and information architecture.
- Whether a new dependency — or a new package, or a new edge in the package graph — enters the project.

### The non-negotiable part: verify before you report

Swift's compiler is a real test. An agent that writes code and reports success without compiling it has
produced a guess. **Before any "done", "implemented", or "fixed" claim, run — from the repo root — and paste
the outcome:**

```bash
cd ios
swift test --package-path Packages/<Name>   # every package you touched
xcodegen generate                           # if ios/project.yml, a Package.swift, or the tree changed
swiftlint lint --strict --quiet             # must exit 0
xcodebuild -project MoniPay.xcodeproj -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17,OS=26.0' \
  build test | xcbeautify
```

**Incorrect (a claim with nothing behind it):**

```
"Added the top-up minimum validation and a typed TopUpError. The build should be fine."
```

**Correct (a claim backed by output):**

```
"Added the top-up minimum validation and a typed TopUpError.
 • swiftlint lint --strict → 0 violations
 • swift test --package-path Packages/TopUp → 12 tests passed
 • xcodebuild build test → BUILD SUCCEEDED, 34 tests passed (iPhone 17 / iOS 26.0)
 • New key added to Packages/TopUp/Sources/TopUp/Resources/Localizable.xcstrings (fr), read with
   bundle: .module"
```

If a command fails and you cannot fix it, report the failure and the error text. A blocked task reported
honestly is far cheaper than a green claim a reviewer has to disprove.

**Review AI output like any other code.** Hallucinated APIs, pre-iOS-17 patterns (`ObservableObject`,
`NavigationView`, `@StateObject`), `Double` money, a `String(localized:)` missing `bundle: .module`, and an
`import` that shortcuts the package graph are the recurring failure modes here. The rules in
`agents/rules/` exist so both humans and agents are held to the same bar.

Reference: [Cal.diy Engineering Blog](https://cal.com/blog/engineering-in-2026-and-beyond)
