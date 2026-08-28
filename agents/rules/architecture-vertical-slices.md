---
title: One Feature, One Local SwiftPM Package
impact: CRITICAL
impactDescription: Compiler-enforced slices: a feature builds, tests, and previews on its own
tags: architecture, vertical-slices, swiftpm, packages, organization, features
---

## One Feature, One Local SwiftPM Package

**Impact: CRITICAL**

MoniPay is organized by domain, not by technical layer, and the organization is enforced by the build system:
each feature is a local SwiftPM package under `ios/Packages/<Feature>/`. A package owns its screens, its
`@Observable` models, its feature-local types, its string catalog, and its tests. `swift build --package-path
ios/Packages/TopUp` must succeed with no other feature present.

**Incorrect (layer-first folders inside one big target):**

```swift
// ios/App/
//   Views/TopUpAmountView.swift
//   Views/CreateCardFormView.swift
//   ViewModels/TopUpViewModel.swift
//   ViewModels/CreateCardViewModel.swift
//   Models/TopUpDraft.swift
```

Adding a step to the top-up flow means editing three unrelated directories; nothing prevents `CreateCardView`
from grabbing `TopUpDraft`; the whole app must compile before a single screen can be previewed.

**Correct (a package per feature):**

```
ios/Packages/TopUp/
  Package.swift
  Sources/TopUp/
    TopUpRoot.swift                    // THE public entry view + its Dependencies struct
    Views/                             // internal screens: AmountStepView, ConfirmStepView, MethodSheet
    Models/                            // internal @Observable state: TopUpModel
    Flows/                             // internal multi-step coordinator: TopUpFlow
    Resources/Localizable.xcstrings    // this feature's copy: en keys, fr translations
  Tests/TopUpTests/                    // mirrors Views/ Models/ Flows/
    Models/TopUpModelTests.swift
```

That internal shape — `Root` / `Views` / `Models` / `Flows` / `Resources` — is mandatory and specified in
`architecture-feature-internal-layout.md`.

The eight feature packages are: `Onboarding` (Splash, Welcome, SignUp), `Home` (Home, Insights,
AuthorizationSheet), `Cards`, `TopUp`, `Convert`, `Transactions`, `KYC`, and `Settings` (+ SubScreens).
`Gallery` is *not* a feature — it is the DesignSystem component catalog and depends on `DesignSystem` alone.

**Exactly one `public` entry point per feature; everything else is `internal`:**

```swift
// ios/Packages/TopUp/Sources/TopUp/TopUpRoot.swift
import DesignSystem
import Money
import SwiftUI
import WalletStore

/// The only symbol ios/App/ needs from this package.
public struct TopUpRoot: View {
    public struct Dependencies: Sendable {
        public var store: Store
        public var suggested: Money?
        public init(store: Store, suggested: Money? = nil) {
            self.store = store
            self.suggested = suggested
        }
    }

    @State private var model: TopUpModel

    public init(dependencies: Dependencies) {
        _model = State(initialValue: TopUpModel(store: dependencies.store,
                                                suggested: dependencies.suggested))
    }

    public var body: some View { TopUpFlow(model: model) }
}
```

```swift
// ios/Packages/TopUp/Sources/TopUp/Models/TopUpModel.swift — no `public`, so no other package can touch it
import Money
import Observation
import WalletStore

@Observable
final class TopUpModel {
    enum Step: Hashable { case amount, confirm, receipt(TopUpReceipt) }

    var step: Step = .amount
    var digits: String = ""
    var method: TopUpMethod = .mtnMoMo

    @ObservationIgnored private let store: Store

    init(store: Store, suggested: Money?) {
        self.store = store
        if let suggested { digits = String(suggested.minorUnits) }
    }

    var amount: Money { Money(minorUnits: Int(digits) ?? 0, currency: .xaf) }
}
```

**What does *not* belong in a feature package:**

| Belongs in the feature package | Belongs elsewhere |
|---|---|
| Screens, sheets, flow state machines | `Money`, `VirtualCard`, `Transaction` → `Money` package |
| Feature-local drafts and enums | Any **reusable** visual element → `DesignSystem` (`MPButton`, `Chip`, `Rule`, `Keypad`) |
| Joining a domain case to a design token (`TxStatus+Style.swift`) | Tokens themselves — colours, type ramp, spacing, radii → `DesignSystem/Tokens/` |
| That feature's `Localizable.xcstrings` | DTOs, URLSession, retries → `ApiClient` |
| That feature's `Tests/<Name>Tests/` | `Store`, `Wallet` actor, `SampleData` → `WalletStore` |
| A `#Preview` per screen, fed by `SampleData` | Tab layout, route → view mapping → `ios/App/` |

A feature never inlines a colour, a font size, a radius, or a duration: it composes `DesignSystem`
components and reads `DesignSystem` tokens (`architecture-design-system-library.md`). A visual pattern that
shows up in a second feature is promoted to `DesignSystem` **before that second use ships**.

**Rule of thumb:** a type used by exactly one feature stays `internal` in that package — never promoted to
`Money` or `WalletStore` "just in case". Promote it only when a *second* package genuinely needs it, and
promote it deliberately (see `architecture-feature-boundaries.md`).

**Benefits**
- One directory to read, to review, and to delete when a feature is retired.
- Previews and tests compile a fraction of the app, so the loop is seconds not minutes.
- `internal` is the default, so the boundary is checked by the compiler rather than by review.

Reference: [Organizing your code with local packages](https://developer.apple.com/documentation/xcode/organizing-your-code-with-local-packages)
