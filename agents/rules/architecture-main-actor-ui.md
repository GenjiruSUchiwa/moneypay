---
title: UI and Observable State on @MainActor, Work in Actors
impact: CRITICAL
impactDescription: Eliminates data races at compile time and keeps the authorization path off the main thread
tags: architecture, concurrency, mainactor, sendable, actors, swift6
---

## UI and Observable State on @MainActor, Work in Actors

**Impact: CRITICAL**

Swift 6 strict concurrency is on everywhere (`// swift-tools-version: 6.2`). Isolation is decided **per
package, in `Package.swift`**:

```swift
// UI-bearing packages — DesignSystem, WalletStore, every feature
swiftSettings: [.defaultIsolation(MainActor.self)]   // main actor unless you say otherwise

// Money and ApiClient — NO defaultIsolation: value types and actors that any isolation may call
```

That default is correct for views and `@Observable` models and wrong for everything that does I/O or math on
shared mutable state. (The same switches live in Xcode under **Swift Compiler – Concurrency**; in a package
they are `SwiftSetting`s.)

Swift 6.2 also changed *where* an `async` function runs: it no longer offloads implicitly, it continues on the
actor that called it. So `await` alone never moves work off the main actor — only `@concurrent` (or an actor
hop) does. Rules 2 and 3 below are what actually get work off the main thread.

Three rules:

1. Views, `@Observable` view models, and `Router` — `@MainActor` (the default; state you `@State`/`@Bindable`
   into a view must be mutated on the main actor).
2. Shared mutable state that is *not* UI — `actor`. `Wallet` is already one: holds, balance, decline counter.
3. Everything crossing an isolation boundary — `Sendable`. Value types with `Sendable` members get it for
   free; reference types must be `final let` or `@unchecked Sendable` with a documented lock.

**Incorrect (blocking the main actor, then racing off it):**

```swift
@Observable @MainActor
final class TopUpModel {
    var balanceXAF = 0
    private var holds: [String: Int] = [:]          // ❌ shared auth state living in the UI model

    func refresh() {
        let data = try! Data(contentsOf: url)        // ❌ synchronous I/O freezes the keypad
        balanceXAF = decode(data)
    }

    nonisolated func authorize(_ id: String, _ xaf: Int) -> Bool {
        holds[id] = xaf                               // ❌ data race: nonisolated write to isolated state
        return true
    }
}
```

**Correct (UI on the main actor, money in an actor, one hop between them):**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/Wallet.swift — non-UI shared state, serialized by the actor
public actor Wallet {
    private(set) var balanceXAF: Int
    private(set) var holds: [String: Int] = [:]

    var availableXAF: Int { balanceXAF - holds.values.reduce(0, +) }

    /// Just-in-time funding: the processor gives us ~2 s to answer.
    /// Idempotent — a replayed authId returns the same decision without re-holding.
    func authorize(authId: String, amountUSDCents: Int, limitUSDCents: Int?) -> AuthDecision {
        if holds[authId] != nil { return .approved }
        let needed = fx.xaf(fromUSDCents: amountUSDCents)
        guard availableXAF >= needed else { return .insufficientFunds }
        holds[authId] = needed
        return .approved
    }
}
```

```swift
// ios/Packages/Home/Sources/Home/Models/HomeModel.swift — UI state, main actor, awaits the actor
@Observable @MainActor
final class HomeModel {
    private(set) var balance: Money = .zero(.xaf)
    private(set) var isLoading = false

    private let wallet: any WalletServicing

    init(wallet: any WalletServicing) { self.wallet = wallet }

    func load() async {
        isLoading = true
        defer { isLoading = false }
        do {
            balance = try await wallet.balance()      // suspends; main actor stays responsive
        } catch {
            balance = .zero(.xaf)
        }
    }
}
```

```swift
// The view never spawns detached work; .task ties the lifetime to the view.
struct HomeView: View {
    @State private var model: HomeModel
    var body: some View {
        BalanceHeader(balance: model.balance)
            .task { await model.load() }              // cancelled automatically on disappear
    }
}
```

**Sendable boundaries**

```swift
// Value types: free Sendable, safe to hand to an actor.
struct Money: Hashable, Sendable { let minorUnits: Int; let currency: Currency }

// Repository protocols cross actors, so the protocol itself is Sendable.
protocol CardServicing: Sendable {
    func issue(_ request: IssueCardRequest) async throws(CardError) -> VirtualCard
}

// Never do this to silence a warning:
// @unchecked Sendable final class CardCache { var items: [VirtualCard] = [] }   ❌
// Use an actor instead:
actor CardCache { private var items: [VirtualCard] = [] }
```

**Offloading real CPU work: `nonisolated` + `@concurrent`**

Aggregating a year of transactions or exporting a statement must not block the keypad. Marking the function
`nonisolated` is *not* enough — since Swift 6.2 it would still run on the caller's actor. Add `@concurrent`,
which guarantees the function runs on the concurrent thread pool:

```swift
// ios/Packages/Money/Sources/Money/StatementBuilder.swift
nonisolated struct StatementBuilder {
    /// Runs on the concurrent pool, never on the main actor.
    @concurrent
    func monthlyTotals(_ transactions: [Transaction]) async -> [Month: Money] { … }
}
```

```swift
// ios/Packages/Transactions/Sources/Transactions/Models/InsightsModel.swift
func reload() async {
    let all = try? await store.transactions()
    totals = await StatementBuilder().monthlyTotals(all ?? [])   // `await` marks the hop
}
```

**Isolated conformances**: when a `@MainActor` model conforms to a plain protocol, write the isolation on the
conformance rather than making the requirement `nonisolated`:

```swift
extension TopUpModel: @MainActor Exportable {
    func export() { … }        // may touch main-actor state; the compiler keeps the use on the main actor
}
```

**Checklist**
- No `DispatchQueue.main.async` — use `await MainActor.run { }` only at a genuine C/callback boundary.
- No `Task.detached` in a view. `.task` or `Task { }` inside a main-actor model, which inherits isolation.
- `nonisolated` for code that must not assume an actor; `@concurrent` when it must actually leave one.
- Never `@unchecked Sendable` a class with `var` storage; make it an `actor`.
- Long CPU work goes in a `nonisolated` type with a `@concurrent` method, never inline in a computed property
  the view reads every frame.
- A type declared in `Money` or `ApiClient` must never assume the main actor — those packages carry no
  `defaultIsolation`, so `@MainActor` there is a smell, not a default.

Reference: swiftui-skills → `Swift-Concurrency-Updates.md` (approachable concurrency, default main-actor
isolation, isolated conformances, `@concurrent`) ·
[Swift Concurrency: Isolation and Sendable](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/concurrency/)
