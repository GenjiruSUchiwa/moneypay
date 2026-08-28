---
title: Structured Concurrency for Async Flows
impact: MEDIUM
impactDescription: Prevents leaked polling loops, duplicate top-ups, and work that outlives its screen
tags: patterns, async, task, cancellation, polling, asyncsequence, swift6
---

## Structured Concurrency for Async Flows

**Impact: MEDIUM**

The MoMo top-up is the reference case: we start a Campay collection, the user gets a USSD push on their
phone, and we poll for up to 60 seconds while showing "Validez sur votre téléphone…". If the user closes the
sheet, that loop must die with the screen. If they tap Confirm twice, we must not collect twice.

**Incorrect (unstructured, uncancellable, re-entrant):**

```swift
struct TopUpFlow: View {
    var body: some View {
        MPButton(title: "Confirm") {
            Task.detached {                                  // ❌ escapes the view's lifetime
                let ticket = try await wallet.startTopUp(amount, method: method)
                while true {                                 // ❌ never checks cancellation
                    try await Task.sleep(for: .seconds(2))   // ❌ no deadline
                    let status = try await wallet.topUpStatus(ticket)
                    if status == .succeeded {
                        await MainActor.run { store.balanceXAF += amount }  // ❌ trusting a local guess
                        break
                    }
                }
            }
        }
    }
}
```

Dismiss the sheet and the loop keeps hitting the POC server for ever. Double-tap and you start two Campay
collections.

**Correct (`.task` for lifetime, a guard for re-entrancy, a deadline for the poll):**

```swift
// ios/Packages/TopUp/Sources/TopUp/Models/TopUpModel.swift
@Observable
final class TopUpModel {
    enum Phase: Equatable { case editing, awaitingUSSD, succeeded(Money), failed(String) }
    private(set) var phase: Phase = .editing

    @ObservationIgnored private let wallet: any WalletServicing
    @ObservationIgnored private var inFlight: Task<Void, Never>?

    init(wallet: any WalletServicing) { self.wallet = wallet }

    /// Idempotent at the UI level: a second tap while a collection is running is a no-op.
    func confirm(_ amount: Money, method: TopUpMethod) {
        guard inFlight == nil else { return }
        inFlight = Task { [weak self] in
            await self?.run(amount, method)
            self?.inFlight = nil
        }
    }

    func cancel() {
        inFlight?.cancel()
        inFlight = nil
    }

    private func run(_ amount: Money, _ method: TopUpMethod) async {
        do {
            let ticket = try await wallet.startTopUp(amount, method: method)
            phase = .awaitingUSSD
            let credited = try await awaitConfirmation(ticket)
            phase = .succeeded(credited)
        } catch is CancellationError {
            phase = .editing                       // user left; say nothing
        } catch WalletError.belowMinimum(let min) {
            phase = .failed(String(localized: "Minimum \(min.formatted()) in demo mode.", bundle: .module))
        } catch {
            phase = .failed(String(localized: "Top-up unavailable. Try again.", bundle: .module))
        }
    }

    /// Poll with a hard 60 s deadline. Every iteration is a cancellation point.
    /// Untyped `throws`: this can fail with `WalletError` *or* `CancellationError`,
    /// so a typed `throws(WalletError)` would not compile here.
    private func awaitConfirmation(_ ticket: TopUpTicket) async throws -> Money {
        let deadline = ContinuousClock.now + .seconds(60)
        while ContinuousClock.now < deadline {
            try await Task.sleep(for: .seconds(2))      // throws CancellationError on dismiss
            switch try await wallet.topUpStatus(ticket) {
            case .succeeded(let credited): return credited
            case .failed(let reason):      throw WalletError.providerRejected(reason)
            case .pending:                 continue
            }
        }
        throw WalletError.timedOut          // mapped to localized copy in the view
    }
}
```

```swift
struct ConfirmStep: View {
    @Bindable var model: TopUpModel

    var body: some View {
        MPButton(title: model.phase == .awaitingUSSD ? "Approve on your phone…" : "Confirm",
                 loading: model.phase == .awaitingUSSD) {
            model.confirm(model.amount, method: model.method)
        }
        // .task ties the work to the view: dismissing cancels it automatically.
        .task(id: model.phase) { await model.refreshBalanceIfNeeded() }
        .onDisappear { model.cancel() }
    }
}
```

**Rules**

1. **`.task` over `onAppear { Task { … } }`.** `.task` cancels on disappear; `onAppear` does not.
2. **`.task(id:)` when the work depends on a value** — changing the id cancels the old task and starts a new
   one. This is how you reload on a changed card selection.
3. **No `Task.detached` in the UI layer.** It drops actor isolation, priority, and cancellation. Use `Task {}`
   inside a main-actor model, which inherits isolation.
   **`await` is not an offload.** Since Swift 6.2 an `async` function runs on the actor that called it, so
   awaiting `wallet.topUpStatus(…)` stays on the main actor until the repository actor hops. That is what we
   want here (the work is I/O). For genuinely CPU-bound work, mark the type `nonisolated` and the method
   `@concurrent` (`architecture-main-actor-ui.md`) — never a `Task.detached` in a view.
4. **Every loop is a cancellation point.** `try await Task.sleep(for:)` throws `CancellationError` on
   cancellation; add `try Task.checkCancellation()` in a loop that does not sleep. Never swallow it with
   `try?` — that turns a cancelled poll into an infinite one.
   Note the typing consequence: a function that can throw both a domain error and `CancellationError` uses
   untyped `throws`. Typed `throws(WalletError)` is for functions that genuinely cannot be cancelled.
5. **Every poll has a deadline.** Use `ContinuousClock`, not a counter of iterations.
6. **Guard re-entrancy on money operations.** One in-flight `Task` handle per operation; a second tap is a
   no-op, not a second collection.
7. **Parallelise independent loads with `async let`**, sequential only when there is a real dependency:
   ```swift
   async let balance = wallet.balance()
   async let cards = cards.cards(for: userID)
   (self.balance, self.cards) = try await (balance, cards)
   ```
8. **Streams over polling when the server supports it.** When webhooks replace polling, `AsyncStream` is the
   shape: `for await event in wallet.events { … }` inside a `.task`, no loop bookkeeping.
9. **Never update a balance from a local guess.** Refetch after a confirmed transition; the server is the
   ledger.

Reference: swiftui-skills → `Swift-Concurrency-Updates.md` (async calls stay on the caller's actor;
`@concurrent` to offload) ·
[Swift Concurrency — Tasks and cancellation](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/concurrency/)
