---
title: NavigationStack with Typed Routes and Explicit Flow State
impact: MEDIUM
impactDescription: Deep links, back gestures, and multi-step money flows behave predictably instead of by luck
tags: patterns, navigation, navigationstack, routes, sheets, flows
---

## NavigationStack with Typed Routes and Explicit Flow State

**Impact: MEDIUM**

`NavigationView` and `isActive` links are gone. Navigation is a **value**: a typed array you can inspect,
test, restore, and deep-link into. A multi-step money flow (TopUp, CreateCard, KYC) is a **state machine**,
not a pile of booleans.

Because feature packages never depend on each other, cross-feature navigation lives in `WalletStore` (which
every feature already depends on) and is resolved to views only in `ios/App/`
(`architecture-feature-boundaries.md`). Navigation *inside* a feature stays `internal` to that package.

**Incorrect (boolean soup, untyped destinations):**

```swift
struct CardsListView: View {
    @State private var showDetail = false
    @State private var selected: VirtualCard?          // ❌ can disagree with showDetail

    var body: some View {
        NavigationView {                                // ❌ deprecated
            List(cards) { card in
                Button(card.label) { selected = card; showDetail = true }
            }
            .sheet(isPresented: $showDetail) { CardDetailView(card: selected!) }  // ❌ crashes on a race
        }
    }
}
```

**Correct (typed routes, one source of truth per surface):**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/AppRoute.swift — the app's whole navigation vocabulary in one enum
public enum AppRoute: Hashable, Sendable {
    case cardDetail(VirtualCard.ID)
    case cardControls(VirtualCard.ID)
    case transaction(Transaction.ID)
}

public enum AppSheet: Hashable, Identifiable, Sendable {
    case topUp(suggested: Money?)
    case createCard
    case kyc
    public var id: Self { self }
}

@Observable
public final class Router {
    public var path: [AppRoute] = []   // typed array: inspectable and testable
    public var sheet: AppSheet?

    public init() {}
    public func push(_ route: AppRoute) { path.append(route) }
    public func popToRoot() { path.removeAll() }
    public func present(_ sheet: AppSheet) { self.sheet = sheet }
}
```

```swift
// ios/App/RootView.swift — the ONLY place routes become views
struct RootView: View {
    @Environment(Router.self) private var router

    var body: some View {
        @Bindable var router = router
        NavigationStack(path: $router.path) {
            MainTabView()
                .navigationDestination(for: AppRoute.self) { route in
                    switch route {
                    case .cardDetail(let id):    CardDetailView(cardID: id)
                    case .cardControls(let id):  CardControlsView(cardID: id)
                    case .transaction(let id):   TransactionDetailView(id: id)
                    }
                }
        }
        .sheet(item: $router.sheet) { sheet in
            switch sheet {
            case .topUp(let suggested): TopUpRoot(dependencies: .init(store: store, suggested: suggested))
            case .createCard:           CardsRoot(dependencies: .init(store: store, mode: .create))
            case .kyc:                  KYCRoot(dependencies: .init(store: store))
            }
        }
    }
}
```

Destinations take **identifiers**, not objects (`CardDetailView(cardID:)`). A route that carries a whole
`VirtualCard` shows a stale snapshot after a freeze and cannot survive a state restoration.

**Multi-step flows are a state machine inside one sheet**, not a nested `NavigationStack`:

```swift
// ios/Packages/Cards/Sources/Cards/Models/CreateCardModel.swift — internal to the package
@Observable
final class CreateCardModel {
    /// One value describes the whole flow. Impossible states are unrepresentable.
    enum Step: Hashable {
        case design                       // theme, network, label
        case limits                       // plafond mensuel, usage unique
        case issuing                      // spinner, Sudo call in flight
        case issued(VirtualCard)          // card created — success screen
        case failed(String)
    }
    private(set) var step: Step = .design

    func back() {
        switch step {
        case .limits: step = .design
        case .failed: step = .limits
        default: break                     // no back out of .issuing — money is moving
        }
    }
}
```

```swift
// Internal to the Cards package; CardsRoot is what ios/App/ sees.
struct CreateCardFlow: View {
    @State private var model: CreateCardModel
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {                                   // one stack per sheet, no nesting
            Group {
                switch model.step {
                case .design:            DesignStep(model: model)
                case .limits:            LimitsStep(model: model)
                case .issuing:           IssuingStep()
                case .issued(let card):  CardCreatedView(card: card) { dismiss() }
                case .failed(let msg):   FailureStep(message: msg, retry: model.retry)
                }
            }
            .navigationTitle("New card")
            .toolbar {
                ToolbarItem(placement: .topBarLeading) {
                    Button("Cancel") { dismiss() }
                        .disabled(model.step == .issuing)   // never abandon a live issuance
                }
            }
            .animation(.easeOut(duration: 0.22), value: model.step)
        }
        .interactiveDismissDisabled(model.step == .issuing)
    }
}
```

**Rules**
- `NavigationStack` with a typed `path`. Never `NavigationView`, never `isActive:`.
- Every destination is registered once, in `ios/App/`, via `navigationDestination(for:)`.
- `.sheet(item:)` with an `Identifiable` enum — never a `Bool` plus a nullable payload.
- Destinations take IDs; the view fetches the current value from the session or repository.
- A flow's steps are one `enum` in its model; the view is a `switch`. No `step1Done`/`step2Done` flags.
- A feature's own routes are `internal`; only the shared `AppRoute`/`AppSheet` vocabulary is `public`.
- Toolbars use iOS 26 grouping: give items an `id`, separate groups with `ToolbarSpacer(.fixed)` /
  `ToolbarSpacer(.flexible)` instead of hand-rolled `Spacer()`s, and use `DefaultToolbarItem(kind:placement:)`
  for system items (search, sidebar) rather than re-implementing them.
- Block dismissal (`interactiveDismissDisabled`, disabled Cancel) while a money operation is in flight.
- Deep links map a URL to an `AppRoute` and assign `router.path`; nothing else changes.

Reference: swiftui-skills → `SwiftUI-New-Toolbar-Features.md` (`ToolbarSpacer`, `DefaultToolbarItem`,
`sharedBackgroundVisibility`) ·
[Migrating to new navigation types](https://developer.apple.com/documentation/swiftui/migrating-to-new-navigation-types)
