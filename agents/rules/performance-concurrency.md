---
title: Never Block the Main Actor; Use Structured Concurrency
impact: HIGH
impactDescription: Main-actor blocking is the top cause of visible hangs; actor hopping in loops is the top cause of slow ones
tags: performance, concurrency, actors, mainactor, swift6
---

## Never Block the Main Actor; Use Structured Concurrency

**Impact: HIGH**

The target builds with Swift 6 strict concurrency and MainActor-by-default isolation. That gives us safety
for free, but it also means most code is *already* on the main actor — so any synchronous work you write
there blocks the UI. Two failure modes matter: blocking the main actor, and hopping across isolation
boundaries inside a loop.

### 1. Nothing slow or blocking on the main actor

**Incorrect (semaphore-blocked main thread — a guaranteed hang):**

```swift
@MainActor
func loadCards() -> [VirtualCard] {
    let semaphore = DispatchSemaphore(value: 0)          // ❌ blocks the main thread
    var result: [VirtualCard] = []
    Task {
        result = (try? await api.cards()) ?? []
        semaphore.signal()
    }
    semaphore.wait()                                     // ❌ potential deadlock
    return result
}
```

**Correct (async all the way; heavy decoding off the main actor):**

```swift
// ios/Packages/ApiClient/Sources/ApiClient/CardsEndpoint.swift
/// Decoding and mapping run off the main actor; only the final state hops back.
nonisolated func fetchCards() async throws(CardsError) -> [VirtualCard] {
    let (data, _) = try await session.data(from: endpoint)
    return try decoder.decode([CardDTO].self, from: data).map(VirtualCard.init)
}

@MainActor
func refresh() async {
    do { cards = try await fetchCards() }
    catch { errorMessage = error.userMessage }
}
```

In a view, prefer `.task { await model.refresh() }` over `.onAppear { Task { … } }`: `.task` is cancelled
automatically when the view disappears, so a scrolled-past screen stops doing work.

### 2. Do not hop actors inside a loop

Every `await` on an `actor` from outside is a suspension and a context switch. Doing it per element turns a
microsecond of work into milliseconds of scheduling.

**Incorrect (one hop into `Wallet` per transaction):**

```swift
var total = 0
for tx in transactions {
    total += await wallet.debitedAmount(for: tx.id)      // ❌ N actor round-trips
}
```

**Correct (one hop, batch the work inside the actor):**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/Wallet+Batch.swift
extension Wallet {
    /// A single boundary crossing; the loop runs inside the actor.
    func debitedAmounts(for ids: [TransactionID]) -> [TransactionID: Int] {
        var result: [TransactionID: Int] = [:]
        result.reserveCapacity(ids.count)
        for id in ids { result[id] = ledger[id]?.debitXAF ?? 0 }
        return result
    }
}

let amounts = await wallet.debitedAmounts(for: transactions.map(\.id))
let total = amounts.values.reduce(0, +)
```

### 3. Structured concurrency over detached tasks

Use `async let` for a fixed number of independent calls, and `withTaskGroup` for a dynamic number. Both
propagate cancellation and errors; `Task.detached` does neither and loses the caller's context.

```swift
// Two independent calls, in parallel, cancelled together.
async let balance = api.balance()
async let cards   = api.cards()
let dashboard = try await Dashboard(balance: balance, cards: cards)

// Variable count: bound the concurrency, don't open 200 sockets.
let statuses = try await withThrowingTaskGroup(of: (CardID, CardStatus).self) { group in
    for card in cards.prefix(8) { group.addTask { (card.id, try await api.status(card.id)) } }
    return try await group.reduce(into: [:]) { $0[$1.0] = $1.1 }
}
```

`Task.detached` is justified only for work that must genuinely outlive its caller. Reaching for it to
"escape the main actor" is wrong — mark the function `nonisolated` instead.

### 4. Sendable, honestly

Domain values from the `Money` package (`Money`, `FXRate`, `Transaction`, `VirtualCard`) are `Sendable`
structs of `Sendable`
members — free, and the compiler checks it. `@unchecked Sendable` requires a comment explaining which lock
or actor protects the state; without one it is a defect in review.

### 5. Respect cancellation

Long loops and polling must check it, so a user who leaves the top-up screen stops the work:

```swift
for await status in campay.pollStatus(reference: reference) {
    try Task.checkCancellation()
    if status.isFinal { return status }
}
```

Reference: [Swift.org — Concurrency](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/concurrency/)
