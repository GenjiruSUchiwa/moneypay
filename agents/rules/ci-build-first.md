---
title: Build the Package First, the App Second — Warnings Are Errors
impact: HIGH
impactDescription: A 10-second swift build replaces a 3-minute xcodebuild for 95 % of changes
tags: ci, swiftpm, xcodebuild, concurrency, swift6, warnings
---

## Build the Package First, the App Second — Warnings Are Errors

**Impact: HIGH**

Swift has no separate type-checker: **building is the type check**. Almost all MoniPay code lives in
SwiftPM packages under `ios/Packages/`, so the fast, correct first move is `swift build` on the one
package you touched — not a full `xcodebuild` that boots a simulator and relinks fourteen packages
to tell you about a missing comma.

**The ladder, always in this order:**

```sh
cd ios

# 1. The package you touched: compiling IS the type check. Seconds, not minutes.
#    `swift build` runs on the macOS host, so it only works for the UIKit-free
#    Platform and ApiClient; anything that imports DesignSystem builds through
#    its own scheme on the simulator, from its package directory.
swift build --package-path Packages/Platform
(cd Packages/Money && xcodebuild -scheme Money -destination 'platform=iOS Simulator,name=iPhone 17 Pro' build -quiet)

# 2. Its tests.
(cd Packages/Money && xcodebuild -scheme Money -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test -quiet)

# 3. The packages that depend on it (Money → WalletStore → features).
(cd Packages/WalletStore && xcodebuild -scheme WalletStore -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test -quiet)

# 4. Lint (the linter catches what the compiler lets through).
swiftlint --strict

# 5. The app, only now — and mandatory if App/, Config/ or project.yml moved.
#    The project is generated: regenerate first.
xcodegen generate
xcodebuild build -project MoniPay.xcodeproj -scheme MoniPay \
  -destination 'platform=iOS Simulator,name=iPhone 17 Pro,OS=26.4' -quiet
```

Steps 1–4 need no generated Xcode project (a package scheme comes from `Package.swift` itself). Step 5 is what CI's main tier runs, and it is the
only step that proves the composition root still links.

**Warnings are build failures.** The project builds in Swift 6 language mode with
`SWIFT_STRICT_CONCURRENCY: complete`; a concurrency warning is the compiler reporting a possible
data race and letting you through only for source compatibility. In a wallet app that is not
stylistic. `ios/project.yml` carries it for the app, and **every `Package.swift` must carry the
matching settings for its own target** — a package compiled by SwiftPM does not inherit the Xcode
project's build settings:

```swift
// ios/Packages/Money/Package.swift
.target(
    name: "Money",
    swiftSettings: [
        .swiftLanguageMode(.v6),
        .treatAllWarnings(as: .error),
        .enableUpcomingFeature("ExistentialAny"),
    ]
)
```

**Incorrect (silencing the diagnostic instead of fixing the isolation):**

```swift
// "Sending 'store' risks causing data races" → the warning is bulldozed.
final class BalanceRefresher {
    nonisolated(unsafe) static var shared = BalanceRefresher()   // lying to the compiler
    var store: Store?

    func refresh() {
        Task { @MainActor in
            self.store?.balanceXAF = 0    // captures non-isolated state
        }
    }
}
```

**Correct (say where the state lives; the compiler then has nothing to warn about):**

```swift
import ApiClient
import WalletStore

/// UI state lives on the main actor; networking lives in a dedicated actor.
@MainActor @Observable
public final class BalanceRefresher {
    private let api: any TopUpService     // Sendable, safe to cross actor boundaries
    private let store: Store

    public init(api: any TopUpService, store: Store) {
        self.api = api
        self.store = store
    }

    public func refresh(userID: String) async {
        // `await` crosses the boundary explicitly; no unprotected shared state.
        guard let snapshot = try? await api.currentUser(userID: userID) else { return }
        store.balanceXAF = snapshot.fcfa
    }
}
```

**Reading the logs.** `swift build` output is short and readable as-is. `xcodebuild` is not:

```sh
xcodebuild build ... 2>&1 | grep -E "error:|warning:" | sort -u
# No output = clean build. Otherwise fix it before going further.
```

**Before opening a PR**, the whole ladder passes locally. CI mirrors it exactly: a `package-tests`
tier running `swift test --package-path ios/Packages/<Name>` per package in parallel, then a
`full-build` tier running `xcodegen generate` + `xcodebuild build` on a freshly generated project.

Reference: [Swift 6 strict concurrency migration](https://www.swift.org/migration/documentation/migrationguide/)
