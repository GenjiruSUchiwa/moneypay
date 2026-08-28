---
title: Import Only What the File Needs; Keep Package Boundaries Honest
impact: MEDIUM
impactDescription: Wrong imports leak SwiftUI into the domain and create illegal package edges
tags: quality, imports, packages, access-control, file-organisation
---

## Import Only What the File Needs; Keep Package Boundaries Honest

**Impact: MEDIUM**

The app is a set of local SwiftPM packages under `ios/Packages/`. An `import` is therefore both a compile
cost and an architectural statement: it declares an edge in the dependency graph. `import SwiftUI` in
`ios/Packages/Money/Sources/Money/Wallet.swift` says "the money domain depends on the UI framework", which
makes the type untestable outside an app host. `import Cards` from inside `TopUp` says "these two features
are coupled", which the layering forbids.

### Framework imports

- `Money`, `ApiClient`, `Platform`: `import Foundation` only. Never `SwiftUI`, never `UIKit`.
- `DesignSystem` and feature packages (`Home`, `Cards`, `TopUp`, …): `import SwiftUI`. It re-exports the
  Foundation types views need, so a second `import Foundation` is redundant noise.
- `import UIKit` only inside a `UIViewRepresentable` bridge or for a genuinely UIKit-only API
  (`UIPasteboard` to copy a PAN, `UIImpactFeedbackGenerator`). Never in `Money` or `ApiClient`.
- `import os` for `Logger`; `import CryptoKit`, `import LocalAuthentication`, `import SwiftData` only in the
  file that actually uses them.

### Cross-package imports follow the dependency direction

`Platform → DesignSystem → Money → ApiClient → WalletStore → Features → App`. A package may import only
packages above it, and it must also be declared in that package's `Package.swift` — an import that compiles
because a transitive dependency happens to expose it is still wrong. **Feature packages never import each
other**; shared UI goes to `DesignSystem`, shared state to `WalletStore`.

**Incorrect (illegal edge + framework leak + unused imports):**

```swift
// ios/Packages/TopUp/Sources/TopUp/TopUpFlow.swift
import SwiftUI
import Cards              // ❌ feature → feature: forbidden
import Combine            // ❌ unused: we use async/await and @Observable
import UIKit              // ❌ unused
```

**Correct:**

```swift
// ios/Packages/TopUp/Sources/TopUp/TopUpFlow.swift
import SwiftUI
import DesignSystem       // Theme, Components, Keypad
import Money              // Money, Currency, TopUpMethod
import WalletStore        // Store, Wallet

public struct TopUpFlow: View { /* ... */ }
```

```swift
// ios/Packages/WalletStore/Sources/WalletStore/Wallet.swift
import Foundation
import Money
import os

actor Wallet {
    private let logger = Logger(subsystem: "com.moneypay.app", category: "wallet")
}
```

### Access control: `public` only at the package boundary

A package's default is `internal`. Widen deliberately:

- `public` — the API another package genuinely calls: `TopUpFlow`, `Money`, `Store`, `Theme`. Public types
  need a doc comment and a `public init` (the memberwise init of a `public struct` is `internal`).
- `package` — visible to every target in the same package, including `Tests` and `<Name>TestSupport`,
  without exposing it to other packages. Use it instead of `public` for anything the test support target
  needs but callers must not touch.
- `internal` (implicit) — everything else.
- `private` / `fileprivate` — file-local helpers.

```swift
public struct Money: Hashable, Sendable {
    public let minorUnits: Int
    public let currency: Currency
    public init(minorUnits: Int, currency: Currency) { … }   // otherwise unusable outside the package

    /// Visible to MoneyTests and MoneyTestSupport, but not to feature packages.
    package var debugDescription: String { "\(minorUnits) \(currency)" }
}
```

`@testable import Money` belongs in `Tests/MoneyTests/` only. Never make something `public` so a test can
reach it — use `package`, or `@testable`.

### File organisation

One primary type per file, named after it (`Wallet.swift`, `TopUpFlow.swift`, `HomeModel.swift`). Stored
properties and `init` first, then behaviour. Group conformances in extensions and use `// MARK: -` so the
Xcode jump bar is useful:

```swift
// MARK: - Model
public struct VirtualCard: Identifiable, Sendable { /* stored properties + init */ }

// MARK: - Derived values
extension VirtualCard {
    public var last4: String { String(pan.suffix(4)) }
}

// MARK: - Codable
extension VirtualCard: Codable { }
```

Markers are English too, like every other comment — `// MARK: - Card`, not `// MARK: - Carte`. See
`quality-english-only-code.md`.

Reference: [Swift Package Manager — Package structure](https://docs.swift.org/swiftpm/documentation/packagemanagerdocs/)
