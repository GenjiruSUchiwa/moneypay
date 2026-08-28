---
title: Inject Dependencies; Ship Doubles in TestSupport Targets
impact: MEDIUM
impactDescription: Every package builds, previews, and tests with no network, no clock, and no POC server
tags: patterns, dependency-injection, testsupport, protocols, testing, previews, swiftpm
---

## Inject Dependencies; Ship Doubles in TestSupport Targets

**Impact: MEDIUM**

A model that constructs its own `LiveWalletService` cannot be previewed offline, cannot be tested without the
POC server on `:8743`, and cannot be pinned to a fixed date. Dependencies arrive through the initializer as
**protocol existentials**; app-wide state arrives through the **SwiftUI Environment**; and every package that
owns an abstraction **exports its doubles from `Sources/<Name>TestSupport/`**.

**Incorrect (singletons and concrete types):**

```swift
// ios/Packages/Cards/Sources/Cards/Models/CreateCardModel.swift
@Observable
final class CreateCardModel {
    private let cards = LiveCardService.shared     // ❌ concrete + global
    private let now = Date()                       // ❌ untestable wall clock
}
```

`#Preview` now performs live HTTP against the Sudo sandbox; a test that runs twice issues two real cards;
"expires in 3 years" cannot be asserted.

**Correct (protocols named `Xxxing`, injected through `init`):**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/CardServicing.swift
public protocol CardServicing: Sendable {
    func issue(_ request: IssueCardRequest) async throws(CardError) -> VirtualCard
    func freeze(id: VirtualCard.ID) async throws(CardError)
}
```

```swift
// ios/Packages/Cards/Sources/Cards/Models/CreateCardModel.swift
import Money
import Platform
import WalletStore

@Observable
final class CreateCardModel {
    enum State { case editing, issuing, issued(VirtualCard), failed(String) }
    private(set) var state: State = .editing

    @ObservationIgnored private let cards: any CardServicing
    @ObservationIgnored private let clock: any Clock          // Platform.Clock

    init(cards: any CardServicing, clock: any Clock) {
        self.cards = cards
        self.clock = clock
    }

    func issue(_ request: IssueCardRequest) async {
        state = .issuing
        do { state = .issued(try await cards.issue(request)) }
        catch CardError.insufficientWalletBalance(let available) {
            state = .failed(String(localized: "Insufficient balance: \(available.formatted()) available.",
                                   bundle: .module))
        } catch { state = .failed(String(localized: "Card creation unavailable.", bundle: .module)) }
    }
}
```

**App-wide state travels through the Environment** (`@Observable` + `.environment(_:)`, never
`@EnvironmentObject`), and `ios/App/` is the only place concrete types are named:

```swift
// ios/App/MoniPayApp.swift — the composition root
import ApiClient
import Platform
import WalletStore

@main
struct MoniPayApp: App {
    @State private var store = Store(
        wallet: LiveWalletService(client: .fromConfiguration()),   // API_BASE_URL via Info.plist APIBaseURL
        cards:  LiveCardService(client: .fromConfiguration()),
        clock:  SystemClock()
    )
    @State private var router = Router()

    var body: some Scene {
        WindowGroup {
            RootView().environment(store).environment(router).tint(Brand.ink)
        }
    }
}
```

**Doubles live in `TestSupport` targets, not in test files**

`Platform` already exports `FixedClock`, `InMemoryKeyValueStoring`, and `SilentLogging`. Every package that
declares an abstraction does the same, so consumers never rewrite the same stub:

```swift
// ios/Packages/Platform/Sources/PlatformTestSupport/FixedClock.swift
import Foundation
import Platform

/// A clock pinned to a fixed instant, so a test can assert an expiry date.
public struct FixedClock: Clock {
    public let now: Date
    public init(now: Date = Date(timeIntervalSince1970: 0)) { self.now = now }
}
```

```swift
// ios/Packages/WalletStore/Sources/WalletStoreTestSupport/StubCardServicing.swift
import Money
import WalletStore

public struct StubCardServicing: CardServicing {
    public var result: Result<VirtualCard, CardError>
    public init(result: Result<VirtualCard, CardError> = .success(.preview)) { self.result = result }
    public func issue(_ request: IssueCardRequest) async throws(CardError) -> VirtualCard { try result.get() }
    public func freeze(id: VirtualCard.ID) async throws(CardError) {}
}
```

```swift
// ios/Packages/Cards/Package.swift
.testTarget(name: "CardsTests", dependencies: [
    "Cards",
    .product(name: "PlatformTestSupport",   package: "Platform"),
    .product(name: "WalletStoreTestSupport", package: "WalletStore"),
])
```

```swift
// ios/Packages/Cards/Tests/CardsTests/CreateCardModelTests.swift
import Money
import PlatformTestSupport
import Testing
import WalletStoreTestSupport
@testable import Cards

@Test @MainActor
func issuingBelowMinimumShowsTheMinimum() async {
    let model = CreateCardModel(
        cards: StubCardServicing(result: .failure(.belowMinimumFunding(Money(minorUnits: 300, currency: .usd)))),
        clock: FixedClock()
    )
    await model.issue(.preview)
    guard case .failed(let message) = model.state else { Issue.record("expected failure"); return }
    #expect(message.contains("3"))
}
```

**Rules**
- No `.shared` for anything we own. `URLSession.shared` is fine; `CardServicing.shared` is not.
- Dependencies are `any Xxxing`, never a concrete class.
- Concrete `Live*` types appear only in `ApiClient` (definition) and `ios/App/` (wiring). Grep for `Live`
  under `ios/Packages/<Feature>/` — there should be no hits.
- Doubles other packages need go in `<Name>TestSupport` (a real product: no `Testing`/`XCTest` import).
  Doubles only one package needs stay `internal` in its own `Tests/`.
- Every `#Preview` injects stubs and `SampleData`, so previews render with no network and no POC server.
- Never `@testable import` across packages — that is what `TestSupport` and `public` are for.
- Environment carries *session and services*; per-screen state stays in `@State`.

Reference: [Migrating from ObservableObject to Observable](https://developer.apple.com/documentation/swiftui/migrating-from-the-observable-object-protocol-to-the-observable-macro)
