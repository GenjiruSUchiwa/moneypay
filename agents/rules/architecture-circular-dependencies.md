---
title: SwiftPM Forbids Cycles — Break Them by Moving Code Down
impact: CRITICAL
impactDescription: A cycle is a hard build failure; the fix is always a layer move, never a new dependency
tags: architecture, dependencies, circular, swiftpm, packages
---

## SwiftPM Forbids Cycles — Break Them by Moving Code Down

**Impact: CRITICAL**

SwiftPM builds a directed acyclic graph. Add `.package(path: "../TopUp")` to `Cards` while `TopUp` already
depends on `Cards` and the build stops with `cyclic dependency declared`. That error is a feature: it catches
in seconds the coupling that a single-target app would hide for months.

The graph must stay this shape (`architecture-layering.md`):

```
Platform ─┐
DesignSystem ─┤  (depend on nothing of ours)
Money ────────┘
   ↑
ApiClient      → Money, Platform
   ↑
WalletStore    → Money, ApiClient, Platform
   ↑
Features/*     → DesignSystem, Money, WalletStore   (never each other)
   ↑
ios/App/       → everything
```

**The three cycles you will actually hit**

### 1. Feature ⇄ Feature

```swift
// ios/Packages/TopUp/Package.swift
.package(path: "../Cards"),      // ❌ "after a top-up, refresh the card list"

// ios/Packages/Cards/Package.swift
.package(path: "../TopUp"),      // ❌ "if funding is short, open top-up"
//  → error: cyclic dependency between 'TopUp' and 'Cards'
```

**Fix: move the shared concept down into `WalletStore`, which both already depend on.**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/WalletEvent.swift
public enum WalletEvent: Hashable, Sendable {
    case toppedUp(Money)
    case cardIssued(VirtualCard.ID)
    case authorizationDeclined(AuthDecision)
}

@Observable
public final class Store {
    private let events = AsyncStream<WalletEvent>.makeStream()
    public var eventStream: AsyncStream<WalletEvent> { events.stream }
    public func emit(_ event: WalletEvent) { events.continuation.yield(event) }
}
```

```swift
// TopUp publishes a fact; it does not know who listens.
store.emit(.toppedUp(credited))

// Cards reacts to the fact; it does not know who produced it.
.task {
    for await event in store.eventStream where event.affectsCards { await model.reload() }
}
```

### 2. Lower package reaching up

```swift
// ios/Packages/Money/Sources/Money/FXRate.swift
import ApiClient                             // ❌ Money is below ApiClient
public struct FXRate { func refresh() async { rate = try await RateClient().fetch() } }
```

**Fix: invert with a protocol.** `Money` declares what it needs; `ApiClient` conforms.

```swift
// ios/Packages/Money/Sources/Money/RateProviding.swift
public protocol RateProviding: Sendable {
    func currentRate() async throws -> FXRate
}

// ios/Packages/ApiClient/Sources/ApiClient/LiveRateProvider.swift
import Money
public struct LiveRateProvider: RateProviding { … }
```

The protocol lives with the *consumer*, the conformance with the *implementer*. That is how a lower layer
"calls" a higher one without depending on it.

### 3. Target cycle inside one package

```swift
.target(name: "TopUp", dependencies: ["TopUpTestSupport"]),      // ❌
.target(name: "TopUpTestSupport", dependencies: ["TopUp"]),      // ❌ cycle
```

`TestSupport` depends on its package, never the reverse. Production code must not reference a double.

**Retain cycles are the runtime version of the same bug**

```swift
// ❌ the whole flow leaks
final class TopUpModel { var receipt: ReceiptModel? }
final class ReceiptModel { var parent: TopUpModel? }

// ✅ the child reports back through a closure
final class ReceiptModel {
    let onDone: () -> Void
    init(onDone: @escaping () -> Void) { self.onDone = onDone }
}
```

Same rule inside a `Task`: `Task { [weak self] in … }` when the task can outlive the model.

**Rules**

1. No `.package(path:)` from a feature package to another feature package. Ever.
2. No `import` of a higher-layer module from a lower one — invert with a protocol instead.
3. When two types genuinely need each other, one of them is misplaced. Move the shared part **down**; do not
   add a dependency **sideways**.
4. `<Name>TestSupport` depends on `<Name>`, never the reverse.
5. Review every `Package.swift` diff. A new dependency line is an architecture change and deserves the same
   scrutiny as a schema migration.

Reference: [Swift Package Manager — dependencies](https://docs.swift.org/package-manager/PackageDescription/PackageDescription.html#package-dependency)
