---
title: Swift Code Review Checklist
impact: HIGH
impactDescription: A shared checklist makes reviews consistent instead of personality-driven
tags: quality, code-review, checklist, swift, swiftui
---

## Swift Code Review Checklist

**Impact: HIGH**

Work through this on every PR. Anything marked **blocking** must be fixed before merge.

### Money and domain correctness — **blocking**

- [ ] No `Double`/`Float` anywhere near an amount. `Decimal` for rates and computations, `Int` minor units
      for stored balances. XAF has 0 decimals, USD has 2.
- [ ] Rounding direction is explicit and justified (`NSDecimalRound(&r, &x, 0, .up)`), not incidental.
- [ ] Currency travels with the amount (`Money`), never as a bare `Int`.
- [ ] Authorisation / capture / void paths stay idempotent — a replayed `authId` must not double-hold.
- [ ] No PAN, CVV, MoMo token, or API key in `print`, `Logger` (`.public`), or an error message.

### Optionals and types — **blocking**

- [ ] No `!`, `try!`, `as!`, or implicitly unwrapped optionals.
- [ ] No `Any` / `[String: Any]` past the decoding boundary.
- [ ] Statuses and kinds are enums, not `String`s; `switch` is exhaustive with no `default:` that would
      swallow a future case.
- [ ] Errors are a domain enum thrown with typed throws; nothing is swallowed by `try?` or an empty `catch`.

### Concurrency — **blocking**

- [ ] No blocking work on the main actor: no synchronous file/network I/O, no JSON decoding of a large
      payload, no crypto in a view body or a `@MainActor` method.
- [ ] Mutable shared state is inside an `actor` (like `Wallet`) or is `@MainActor`-isolated.
- [ ] Types crossing isolation boundaries are `Sendable`; no `@unchecked Sendable` without a written reason.
- [ ] `Task { }` inside a view captures `[weak self]` when it holds a model, or uses `.task {}` so SwiftUI
      cancels it on disappear.
- [ ] No `Task.detached` used merely to escape the main actor.

### Memory

- [ ] Escaping closures stored on a model use `[weak self]`; `guard let self else { return }` first line.
- [ ] No retain cycle between `Store` and a feature model, or via a closure held in a property.
- [ ] Long-lived `AsyncStream`/`Observations` subscriptions are cancelled when their owner goes away.

### SwiftUI

- [ ] `body` is cheap: no sorting, filtering, formatting a whole list, date math, or I/O. Precompute in the
      model. See `performance-swiftui-body.md`.
- [ ] `ForEach` uses a stable identity (`Identifiable` or `id: \.id`), never `id: \.self` on a mutable value
      and never array indices for a reorderable list.
- [ ] Long lists use `LazyVStack` / `List`, not `VStack` in a `ScrollView`.
- [ ] `@Observable` + `@Bindable`; no `ObservableObject` / `@Published` / `@StateObject` in new code.
- [ ] `@State` owns value types or `@Observable` models created in the view; `@Environment` for shared ones.
- [ ] `NavigationStack` with a typed path; no `NavigationView`.
- [ ] Liquid Glass usage grouped in a `GlassEffectContainer`, not one `.glassEffect()` per leaf.

### Accessibility — **blocking for new screens**

- [ ] Icon-only buttons have `.accessibilityLabel(...)`; decorative images are `.accessibilityHidden(true)`.
- [ ] Amounts read correctly: `.accessibilityLabel("Solde : 428 500 francs CFA")`, not "428500 FCFA".
- [ ] Tap targets ≥ 44×44 pt.
- [ ] Layout survives Dynamic Type at XXL — no fixed `.frame(height:)` around text.
- [ ] Colour is never the only signal (a declined transaction needs the ✕ symbol, not just red).

### Design-system reuse — **blocking**

- [ ] No raw visual literal in a feature package: no `Color(red:` / hex / `#colorLiteral`, no
      `.font(.system(size:))`, no magic `.padding(16)` or `cornerRadius:`. Use `.surface`, `.action`,
      `Spacing.md`, `Typography.title`, `Radii.card`, `Motion.standard`.
- [ ] Nothing re-implements an existing `DesignSystem` component (button, row, pill, chip, keypad, meter).
      Check `Components/<Family>/` and the `Gallery` catalog first.
- [ ] A visual pattern now used by a **second** feature is promoted into `DesignSystem` in this PR — one
      file under `Components/<Family>/`, `public` type + labelled `public init`, variants as enums,
      accessibility label/traits, a `#Preview` of every variant, a `///` doc comment, a `Gallery` screen.
- [ ] New tokens are semantic (`.inkQuiet`), never appearance-named (`.grey3`), and raw values stay inside
      `DesignSystem/Sources/DesignSystem/Tokens/`.
- [ ] Feature internals follow the layout: `<Feature>Root.swift`, `Views/`, `Models/`, `Flows/`,
      `Resources/`. See `quality-no-inline-styling.md`.

### Localization — **blocking**

- [ ] No hardcoded user-facing string. Everything through `Text("…")` / `String(localized:)` backed by
      the owning package's `Localizable.xcstrings`, and every call inside `Packages/` passes
      `bundle: .module`. See `quality-localization.md`.
- [ ] Source keys are English; the French is the `fr` translation in the catalog. No French key.
- [ ] Amounts and dates use `FormatStyle` with the user's locale, not `String(format:)`; money is
      `Decimal` + currency code, XAF with 0 fraction digits.
- [ ] No concatenated sentences — one key with placeholders.
- [ ] `DesignSystem` components take `LocalizedStringResource`/`Text`, not an already-resolved `String`.
- [ ] Layout is direction-relative: `.leading`/`.trailing`, `.topLeading`, `chevron.forward` — never
      `.left`/`.right`, and no fixed `.frame(width:)` around text.
- [ ] Accessibility labels go through the catalog and read the *formatted* value.
- [ ] New views have an `fr_CM` `#Preview`; formatting tests pin an explicit `Locale`.
      See `quality-i18n-formatting.md`.

### Package boundaries — **blocking**

- [ ] No feature package imports another feature package. Shared UI → `DesignSystem`, shared state →
      `WalletStore`, shared values → `Money`.
- [ ] Every `import` is declared in that package's `Package.swift`, not inherited transitively.
- [ ] Dependency direction respected: `Platform → DesignSystem → Money → ApiClient → WalletStore →
      Features → App`.
- [ ] `public` only where another package genuinely calls it; `package` for test-support visibility;
      `internal` otherwise. A `public struct` that other packages construct has an explicit `public init`.
- [ ] `ios/App/` stays a composition root — no business logic, no feature views defined there.
- [ ] `@testable import` only inside `Packages/<Name>/Tests/`.

### Hygiene

- [ ] No French anywhere in code: identifiers, comments, `// MARK:`, doc comments, test names, log
      messages, API fields, DB columns. See `quality-english-only-code.md`.
- [ ] **Zero warnings, zero suppressions.** `swiftlint lint --strict` and the build are clean *by fixing*,
      not by `swiftlint:disable`, `#pragma warning disable`, `[SuppressMessage]`, `<NoWarn>`, `null!`,
      `@unchecked Sendable`, `try?`, a raised threshold, or a widened `excluded:`. Any suppression must be
      the documented upstream exception, in its own PR. See `quality-zero-warnings.md`.
- [ ] `dotnet build -warnaserror` and `dotnet format --verify-no-changes` clean for backend changes.
- [ ] `swift test --package-path Packages/<Name>` green for every package touched.
- [ ] `xcodebuild build test` green on the iOS 26 simulator.
- [ ] Files under ~400 lines; types under ~200; functions do one thing.
- [ ] Cyclomatic complexity under 10 per function (SwiftLint `cyclomatic_complexity`, CA1502 on the
      backend); nesting ≤ 3. Prefer `guard` + extraction + enum dispatch over an `if`/`else if` chain.
      See `quality-cyclomatic-complexity.md`.
- [ ] `ios/project.yml` and the relevant `Package.swift` updated when a package or target was added;
      `xcodegen generate` rerun. No `MoniPay.xcodeproj` in the diff — it is generated and gitignored.
- [ ] No dead code, no commented-out code, no unowned `TODO`.
- [ ] New `Money` / `WalletStore` logic has Swift Testing coverage (`@Test`, `#expect`, `#require`) inside
      its own package.

### PR size

Over ~500 lines or ~10 files: ask the author to split bottom-up along the package graph — `Money` →
`ApiClient`/`WalletStore` → the feature package → `ios/App/` → tests. Smaller PRs get faster feedback,
cleaner history, and easier rollback.

Reference: [Apple — Accessibility for SwiftUI](https://developer.apple.com/documentation/swiftui/accessibility-fundamentals)
