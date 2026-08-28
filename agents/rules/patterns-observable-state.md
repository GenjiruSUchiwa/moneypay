---
title: State with @Observable, @State Ownership, and @Bindable
impact: MEDIUM
impactDescription: Correct ownership eliminates stale UI, lost state on redraw, and needless view invalidation
tags: patterns, observable, state, bindable, swiftui, derived-state
---

## State with @Observable, @State Ownership, and @Bindable

**Impact: MEDIUM**

iOS 26 has one state system: the `@Observable` macro. `ObservableObject`, `@Published`,
`@StateObject`, `@ObservedObject`, and `@EnvironmentObject` are legacy — do not introduce them. `@Observable`
tracks reads at the property level, so a view that renders only `balance` is not invalidated when
`notifications` changes.

**Incorrect (legacy stack, and state owned by the wrong view):**

```swift
final class TopUpViewModel: ObservableObject {          // ❌ legacy
    @Published var digits = ""                          // ❌ invalidates every observer
}

struct TopUpFlow: View {
    @ObservedObject var model = TopUpViewModel()        // ❌ recreated on every parent redraw → state lost
}
```

**Correct (`@Observable` + `@State` ownership):** the model is `internal` to its package; only the flow view
that `ios/App/` presents is `public`.

```swift
import Observation

// ios/Packages/TopUp/Sources/TopUp/Models/TopUpModel.swift
// The package carries `.defaultIsolation(MainActor.self)`, so no @MainActor annotation is needed.
@Observable
final class TopUpModel {
    // Stored state: the minimum the flow must remember.
    var digits: String = ""
    var method: TopUpMethod = .mtnMoMo
    var step: Step = .amount

    enum Step: Hashable { case amount, confirm, receipt(TopUpReceipt) }

    // Derived state: computed, never stored, never manually synced.
    var amount: Money { Money(minorUnits: Int(digits) ?? 0, currency: .xaf) }
    var fee: Money { method.fee(on: amount) }
    var credited: Money { amount - fee }
    var isValid: Bool { amount >= method.minimum }

    // Not observed: an implementation detail views must not read.
    @ObservationIgnored private let wallet: any WalletServicing

    init(wallet: any WalletServicing) { self.wallet = wallet }
}
```

```swift
struct TopUpFlow: View {
    // The view that CREATES the model owns it with @State. Child views receive it plainly.
    @State private var model: TopUpModel

    init(wallet: any WalletServicing) {
        _model = State(initialValue: TopUpModel(wallet: wallet))
    }

    var body: some View {
        switch model.step {
        case .amount:  AmountStep(model: model)          // plain `let`, no wrapper needed
        case .confirm: ConfirmStep(model: model)
        case .receipt(let receipt): ReceiptStep(receipt: receipt)
        }
    }
}
```

**`@Bindable` only where you need a `Binding` into an `@Observable`:**

```swift
struct AmountStep: View {
    @Bindable var model: TopUpModel                      // gives $model.method

    var body: some View {
        VStack {
            AmountEntry(digits: model.digits, currency: "FCFA")
            MethodPicker(selection: $model.method)        // needs a Binding
        }
    }
}

// From the Environment, bind locally:
struct SettingsView: View {
    @Environment(Store.self) private var session
    var body: some View {
        @Bindable var session = session
        Toggle("Hide balance", isOn: $session.hiddenBalance)
    }
}
```

**Ownership rules**

| Situation | Use |
|---|---|
| This view creates the model | `@State private var model: Model` |
| A parent passes an already-created model | `let model: Model` |
| You need `$` bindings on a passed-in model | `@Bindable var model: Model` |
| App-wide session/services | `@Environment(Store.self)` |
| Trivial local UI flag (sheet shown, focus) | `@State private var …` in the view |

**Derived state is computed, never stored.** Never mirror one property into another and keep them in sync by
hand:

```swift
// ❌ two sources of truth for the same fact
var digits = "" { didSet { amount = Money(minorUnits: Int(digits) ?? 0, currency: .xaf) } }
var amount: Money = .zero(.xaf)

// ✅ one source, one derivation
var digits = ""
var amount: Money { Money(minorUnits: Int(digits) ?? 0, currency: .xaf) }
```

**Other rules**
- UI state is main-actor state. In a UI package that is the manifest default; write `@MainActor` explicitly
  only in a package without `.defaultIsolation(MainActor.self)`.
- `@ObservationIgnored` on injected services and caches so reading them never invalidates a view.
- Models are `final class`. Value-type screen state stays in `@State` — no model needed for a single toggle.
- Expensive derivations (grouping 500 transactions by day) are cached in a stored property updated in one
  place, not recomputed in a `body`-read computed property.
- Never mutate model state from `body`. Mutate in an action closure or a `.task`.

Reference: [Managing model data in your app](https://developer.apple.com/documentation/swiftui/managing-model-data-in-your-app)
