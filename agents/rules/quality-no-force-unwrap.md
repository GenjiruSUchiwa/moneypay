---
title: Never Force-Unwrap, Force-Try, or Force-Cast
impact: CRITICAL
impactDescription: Every `!` is a crash the user sees mid-payment
tags: quality, optionals, safety, crash
---

## Never Force-Unwrap, Force-Try, or Force-Cast

**Impact: CRITICAL**

`!`, `try!`, `as!` and `fatalError()` in production paths turn a recoverable condition into a terminated
process. In a wallet app the crash lands while the user is topping up or authorising a card payment, and the
crash report tells you nothing about *why* the value was missing. Optionals are the compiler telling you a
case exists; answer it, do not silence it.

**Banned in app code:** `!` (force unwrap), `try!`, `as!`, `unsafelyUnwrapped`, implicitly unwrapped
optionals (`var x: Card!`), and `array[index]` where `index` is not proven in range.

**Allowed, with justification in a comment:** `fatalError` in a genuinely unreachable `default` of an
exhaustive switch over a non-frozen enum; force-unwrap of a literal that the compiler cannot fold (rare).
Tests may use `try #require(...)` from Swift Testing instead, which fails the test rather than the process.

**Incorrect (three crashes waiting in eight lines):**

```swift
func selectedCard(in store: Store) -> VirtualCard {
    let card = store.cards.first { $0.id == store.selectedCardID }!   // crash if deselected
    let limit = card.monthlyLimitUSDCents!                            // crash if no limit set
    let url = URL(string: "\(baseURL)/cards/\(card.id)")!             // crash on a bad base URL
    print(limit, url)
    return card
}
```

**Correct (guard, early return, and a typed failure):**

```swift
func selectedCard(in store: Store) throws(WalletError) -> VirtualCard {
    guard let id = store.selectedCardID,
          let card = store.cards.first(where: { $0.id == id })
    else { throw .noCardSelected }
    return card
}

/// nil means no limit, which is a valid state rather than an error.
func remainingBudgetUSDCents(for card: VirtualCard) -> Int? {
    guard let limit = card.monthlyLimitUSDCents else { return nil }
    return max(0, limit - card.spentUSDCents)
}
```

**Safe collection access — never index blindly:**

```swift
extension Collection {
    subscript(safe index: Index) -> Element? {
        indices.contains(index) ? self[index] : nil
    }
}

// Incorrect: transactions[0] crashes on an empty ledger.
// Correct:
if let latest = store.transactions.first { show(latest) }
```

**Casting:** use `as?` with a `guard`, or model the alternatives as an `enum` with associated values so no
cast is needed at all (see `quality-optionals-and-types.md`). `as!` on a decoded payload from Campay or
Sudo Africa is a crash triggered by a third party changing their JSON.

SwiftLint enforces this: `force_unwrapping`, `force_try` and `force_cast` are errors, not warnings.

Reference: [The Swift Programming Language — Optional Chaining](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/optionalchaining/)
