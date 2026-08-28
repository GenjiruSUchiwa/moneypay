---
title: Feature Packages Never Depend on Each Other
impact: CRITICAL
impactDescription: Keeps each feature independently buildable; the public surface is the reviewed contract
tags: architecture, boundaries, swiftpm, access-control, navigation, coupling
---

## Feature Packages Never Depend on Each Other

**Impact: CRITICAL**

`ios/Packages/Cards/Package.swift` must not list `.package(path: "../TopUp")`. Two features that need the
same thing share it downwards — through `Money` (types and rules), `WalletStore` (app state), or
`DesignSystem` (visual primitives). Everything a feature exposes upwards is a `public` API surface, and that
surface is reviewed like an API.

**`internal` is the default and it is load-bearing.** In a SwiftPM package, a type without `public` is
invisible outside the package. Adding `public` is a deliberate act: it means "`ios/App/` needs this".

**Incorrect (a sideways dependency):**

```swift
// ios/Packages/Cards/Package.swift
dependencies: [
    .package(path: "../DesignSystem"),
    .package(path: "../TopUp"),          // ❌ feature → feature
],
```

```swift
// ios/Packages/Cards/Sources/Cards/Flows/CreateCardFlow.swift
import TopUp
struct CreateCardFlow: View {
    var body: some View {
        if store.balance < required {
            TopUpRoot(dependencies: .init(store: store))          // ❌ owns another feature's screen
        }
    }
}
```

Cards now depends on TopUp's step machine, its string catalog, and its release cadence; neither package can be
built or tested alone, and SwiftPM will refuse the day TopUp needs something from Cards.

**Correct — option A: the feature declares an intent, `ios/App/` satisfies it.**

```swift
// ios/Packages/Cards/Sources/Cards/CardsRoot.swift — the package's only public symbol
public struct CardsRoot: View {
    public struct Dependencies: Sendable {
        public var store: Store
        public var onNeedsFunding: (Money) -> Void   // a closure, not another package's type

        public init(store: Store, onNeedsFunding: @escaping (Money) -> Void) {
            self.store = store
            self.onNeedsFunding = onNeedsFunding
        }
    }

    public init(dependencies: Dependencies) { … }
}
```

```swift
// ios/App/RootView.swift — the only place two features meet
CardsRoot(dependencies: .init(store: store,
                              onNeedsFunding: { router.present(.topUp(suggested: $0)) }))
```

**Correct — option B: a shared route vocabulary in `WalletStore` (both features already depend on it).**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/AppRoute.swift
public enum AppSheet: Hashable, Identifiable, Sendable {
    case topUp(suggested: Money?)
    case createCard
    case kyc
    public var id: Self { self }
}

@Observable
public final class Router {
    public var path: [AppRoute] = []
    public var sheet: AppSheet?
    public init() {}
    public func present(_ sheet: AppSheet) { self.sheet = sheet }
}
```

`Cards` calls `router.present(.topUp(suggested: missing))`; only `ios/App/` maps `.topUp` to `TopUpFlow`.

**Rules for the public surface**

1. **One `public` view per feature package** — `<Feature>Root`, plus its nested `Dependencies` struct
   (`architecture-feature-internal-layout.md`). Everything else stays `internal`.
2. **`Dependencies` fields are plain values or closures**: `Money`, `VirtualCard.ID`, `() -> Void`. Never
   another feature's model, view, or enum.
3. **Never `@testable import` across packages.** If a test needs a double, the owning package exports it from
   `Sources/<Name>TestSupport/` (`patterns-dependency-injection.md`).
4. **No `public` on a type only used inside the package.** Reviewers should ask "who outside needs this?" at
   every `public` keyword.
5. **A feature never re-exports its dependencies.** `@_exported import Money` in a feature turns the boundary
   back into mud.
6. If two features need the same view, it moves to `DesignSystem` **before the second use ships**
   (`architecture-design-system-library.md`); the same rule moves to `Money`; the same state moves to
   `WalletStore`. Copy-pasting is not the fix, and neither is a new dependency.

**Enforcement is free**: SwiftPM rejects `Cards → TopUp` the moment it becomes a cycle, and `internal` hides
everything you did not deliberately publish. Review the `Package.swift` diff of every PR — a new
`.package(path: "../<Feature>")` line under `ios/Packages/<Feature>/` is a boundary violation on its face.

Reference: [Access Control](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/accesscontrol/)
