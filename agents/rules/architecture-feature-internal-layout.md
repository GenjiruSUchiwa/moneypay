---
title: Inside a Feature Package — Root, Views, Models, Flows, Resources
impact: HIGH
impactDescription: A flat Sources/ folder hides the public surface and lets views and state blur together
tags: architecture, features, organization, swiftpm, structure
---

## Inside a Feature Package — Root, Views, Models, Flows, Resources

**Impact: HIGH**

A feature package is not a bag of files. Inside `ios/Packages/<Feature>/Sources/<Feature>/` there are exactly
five things, and a reader can tell in one `ls` what the package exposes, what it renders, and what it
remembers.

```
ios/Packages/Cards/Sources/Cards/
  CardsRoot.swift                  // THE public entry view + its Dependencies struct
  Views/                           // internal SwiftUI views — one screen or component per file
    CardsListView.swift
    CardDetailView.swift
    CardControlsView.swift
    CardRow.swift
  Models/                          // internal @Observable state / view models
    CardsListModel.swift
    CardDetailModel.swift
  Flows/                           // internal multi-step coordinators (a step enum + its switch)
    CreateCardFlow.swift
  Resources/
    Localizable.xcstrings

ios/Packages/Cards/Tests/CardsTests/    // mirrors the folders
  Models/CardsListModelTests.swift
  Flows/CreateCardFlowTests.swift
```

**Incorrect (flat `Sources/`, ambiguous surface):**

```
ios/Packages/Cards/Sources/Cards/
  CardsListView.swift        // public? internal? who calls it?
  CardDetailView.swift
  CardControlsView.swift
  CreateCardFlow.swift
  CardsListModel.swift
  Helpers.swift              // ❌ junk drawer
  Extensions.swift           // ❌ junk drawer
```

Nothing marks the entry point, so three types drift to `public` "because App needed one of them"; state and
rendering interleave alphabetically; `Helpers.swift` grows for ever.

**Correct — `<Feature>Root.swift` is the only `public` file:**

```swift
// ios/Packages/Cards/Sources/Cards/CardsRoot.swift
import DesignSystem
import Money
import SwiftUI
import WalletStore

public struct CardsRoot: View {
    /// Everything this feature needs from the outside, named once.
    /// ios/App/ builds it; tests build it with doubles from WalletStoreTestSupport.
    public struct Dependencies: Sendable {
        public var store: Store
        public var cards: any CardServicing
        public var onNeedsFunding: (Money) -> Void

        public init(store: Store,
                    cards: any CardServicing,
                    onNeedsFunding: @escaping (Money) -> Void) {
            self.store = store
            self.cards = cards
            self.onNeedsFunding = onNeedsFunding
        }
    }

    @State private var model: CardsListModel
    private let dependencies: Dependencies

    public init(dependencies: Dependencies) {
        self.dependencies = dependencies
        _model = State(initialValue: CardsListModel(cards: dependencies.cards))
    }

    public var body: some View {
        CardsListView(model: model, onNeedsFunding: dependencies.onNeedsFunding)
    }
}
```

```swift
// ios/App/RootView.swift — App composes; it never names an internal type
CardsRoot(dependencies: .init(store: store,
                              cards: cards,
                              onNeedsFunding: { router.present(.topUp(suggested: $0)) }))
```

**What goes where**

| Folder | Contains | Access |
|---|---|---|
| `<Feature>Root.swift` | one entry `View` + its `Dependencies` struct | `public` |
| `Views/` | screens and feature-only subviews; no business rules, no I/O | `internal` |
| `Models/` | `@Observable` state, derived values, calls into injected services | `internal` |
| `Flows/` | multi-step coordinators: a `Step` enum + the `switch` that renders it | `internal` |
| `Resources/` | `Localizable.xcstrings`, feature-only assets | — |

**Rules**

1. **One `public` symbol per feature package**: `<Feature>Root` (plus its nested `Dependencies`). Everything
   else is `internal` — the compiler then enforces the boundary
   (`architecture-feature-boundaries.md`).
2. **Dependencies are a named struct on the Root**, not a growing list of `init` parameters. Adding a
   dependency is then a visible, reviewable diff in one place.
3. **`Views/` never owns state that outlives a frame** and never calls a service directly; it takes a model
   or plain values and closures.
4. **`Models/` never imports SwiftUI** beyond `Observation` needs — no `View`, no `Color`, no `Font`. That
   keeps model tests headless.
5. **`Flows/` is for multi-step only.** A single screen does not get a flow; a `Step` enum with one case is a
   smell (`patterns-navigation.md`).
6. **No `Helpers.swift`, `Utils.swift`, or `Extensions.swift`.** An extension lives in a file named after
   what it extends (`TxStatus+Style.swift`); if it is generally useful it belongs in `DesignSystem`,
   `Money`, or `Platform`.
7. **Tests mirror the folders.** `Tests/CardsTests/Models/…`, `Tests/CardsTests/Flows/…` — so an untested
   model is visible as a missing file.

Reference: [Organizing your code with local packages](https://developer.apple.com/documentation/xcode/organizing-your-code-with-local-packages)
