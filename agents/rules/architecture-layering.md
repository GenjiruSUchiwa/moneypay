---
title: Package Layers and the Dependency Direction
impact: CRITICAL
impactDescription: Wrong placement makes a package unbuildable in isolation and drags UI frameworks into money code
tags: architecture, layers, swiftpm, packages, dependencies, organization
---

## Package Layers and the Dependency Direction

**Impact: CRITICAL**

MoniPay is a stack of local SwiftPM packages under `ios/Packages/`. A package may depend only on packages
**above it** in this list; SwiftPM enforces it at build time.

```
Platform      Clock, Logging, KeyValueStoring, BundleConfiguration   (+ PlatformTestSupport)
DesignSystem  Tokens/, Foundations/, Components/<Family>/, Resources/   (SwiftUI only, no domain)
Money         Money, Currency, FXRate, VirtualCard, Transaction, TxStatus/Kind/Category,
              TopUpMethod, User, AppNotification                     (Foundation only)
ApiClient     DTOs mirroring poc/README.md, URLSession transport, error mapping
WalletStore   Wallet actor, Store (@Observable app state), repositories, SampleData
Features      Onboarding, Home, Cards, TopUp, Convert, Transactions, KYC, Settings
Gallery       DesignSystem component catalog — depends on DesignSystem ONLY, not a feature
App           ios/App/ — composition root only
```

- `Platform`, `DesignSystem`, and `Money` depend on nothing of ours.
- `ApiClient` depends on `Money` (+ `Platform`). It never imports `DesignSystem`.
- `WalletStore` depends on `Money` + `ApiClient` + `Platform`.
- Feature packages depend on `DesignSystem` + `Money` + `WalletStore` (+ `ApiClient`/`Platform` when needed),
  and **never on each other** (`architecture-feature-boundaries.md`). Their internal shape is fixed:
  `<Feature>Root.swift` + `Views/` + `Models/` + `Flows/` + `Resources/`
  (`architecture-feature-internal-layout.md`).
- `Gallery` sits beside the features but depends on `DesignSystem` alone: it is the component catalog
  (`architecture-design-system-library.md`), so it must keep compiling with no domain package present.
- `ios/App/` depends on everything and is the only place that names concrete implementations.

**Incorrect (layers leaking in both directions):**

```swift
// ios/Packages/Money/Sources/Money/Transaction.swift
import SwiftUI                                     // ❌ Money must not link SwiftUI
extension TxStatus { var tint: Color { … } }       // ❌ pixels in the domain package

// ios/Packages/Money/Sources/Money/Wallet.swift
import ApiClient                                   // ❌ Money is below ApiClient — inverted
```

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/BalanceRow.swift
import Money                                        // ❌ DesignSystem knows the domain
struct BalanceRow: View { let balance: Money }
```

The current root-level `MoneyPay/Domain/Models.swift` does `import SwiftUI` only to hang a `Color` off
`TxStatus`. Under the package layout that line does not compile: `Money` has no SwiftUI dependency, and that
is the point.

**Correct (each package keeps its own concern):**

```swift
// ios/Packages/Money/Sources/Money/Transaction.swift — Foundation only, Sendable, testable headlessly
import Foundation

public enum TxStatus: String, Sendable, CaseIterable { case approved, pending, declined, refunded }

public struct Transaction: Identifiable, Hashable, Sendable {
    public let id: UUID
    public var merchant: String
    public var status: TxStatus
    public var date: Date
    public var presented: Money        // USD minor units
    public var settled: Money          // XAF minor units
    public var cardID: VirtualCard.ID?
}
```

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/Tokens/Colors.swift — pixels, no domain types.
// Tokens/ is the ONLY place a raw colour literal is allowed.
public enum Brand {
    public static let credit  = Color(rgb: 0x1B7F4C)
    public static let debit   = Color(rgb: 0xB8332E)
    public static let pending = Color(rgb: 0xB88A00)
}
```

```swift
// ios/Packages/Transactions/Sources/Transactions/Views/TxStatus+Style.swift
// The FEATURE joins a domain case to a design token. Neither lower package knows the other.
import DesignSystem
import Money
import SwiftUI

extension TxStatus {
    var tint: Color {
        switch self {
        case .approved: Brand.credit
        case .pending:  Brand.pending
        case .declined: Brand.debit
        case .refunded: Brand.mark
        }
    }
}
```

```swift
// ios/Packages/ApiClient/Sources/ApiClient/LiveWalletService.swift — the only URLSession in the repo
import Money
public actor LiveWalletService: WalletServicing {
    public func balance() async throws(WalletError) -> Money {
        let dto: UserDTO = try await client.get("/user")
        return Money(minorUnits: dto.balanceFcfa, currency: .xaf)   // DTO → domain, here
    }
}
```

**Manifest snippets that encode the layers**

```swift
// ios/Packages/Money/Package.swift — no dependencies at all
dependencies: [],
targets: [.target(name: "Money"), .testTarget(name: "MoneyTests", dependencies: ["Money"])]
```

```swift
// ios/Packages/WalletStore/Package.swift
dependencies: [
    .package(path: "../ApiClient"),
    .package(path: "../Money"),
    .package(path: "../Platform"),
],
targets: [
    .target(name: "WalletStore", dependencies: [
        .product(name: "ApiClient", package: "ApiClient"),
        .product(name: "Money",     package: "Money"),
        .product(name: "Platform",  package: "Platform"),
    ], swiftSettings: [.defaultIsolation(MainActor.self)]),
    .target(name: "WalletStoreTestSupport", dependencies: ["WalletStore"]),
    .testTarget(name: "WalletStoreTests", dependencies: [
        "WalletStore",
        .product(name: "PlatformTestSupport", package: "Platform"),
    ]),
]
```

```swift
// ios/Packages/Cards/Package.swift — a feature: DesignSystem + Money + WalletStore, nothing sideways
dependencies: [
    .package(path: "../DesignSystem"),
    .package(path: "../Money"),
    .package(path: "../WalletStore"),
],
```

```swift
// ios/Packages/Gallery/Package.swift — the component catalog: DesignSystem and nothing else
dependencies: [
    .package(path: "../DesignSystem"),
],
```

**Placement table**

| Question | Package |
|---|---|
| "What time is it? Where do I put a byte?" | `Platform` |
| "What does a primary button / a status pill look like?" | `DesignSystem/Components/<Family>/` |
| "What colour is `credit`? What is the base spacing?" | `DesignSystem/Tokens/` |
| "Which token does an *approved* transaction use?" | the feature (`Views/TxStatus+Style.swift`) |
| "What is a `Money`? Can this card be charged?" | `Money` |
| "How do I reach the POC backend?" | `ApiClient` |
| "What is the current balance and card list?" | `WalletStore` |
| "What happens when the user taps *Recharger*?" | `TopUp` |
| "Which tab is up? Which concrete service is wired?" | `ios/App/` |

**Litmus test:** `swift build --package-path ios/Packages/<Name>` must succeed for every package on its own.
If a package needs a sibling feature to compile, it is in the wrong layer.

Reference: [Organizing your code with local packages](https://developer.apple.com/documentation/xcode/organizing-your-code-with-local-packages)
