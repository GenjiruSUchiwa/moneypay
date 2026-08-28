---
title: Avoid O(n²) — Reach for Set and Dictionary
impact: CRITICAL
impactDescription: Quadratic ledger scans freeze the UI once a user has real transaction history
tags: performance, algorithms, complexity, collections
---

## Avoid O(n²) — Reach for Set and Dictionary

**Impact: CRITICAL**

A demo account (`SampleData` in `WalletStore`) has 20 transactions and 3 cards. A real MoniPay user after a year has thousands of
transactions and dozens of cards, and this code runs inside a SwiftUI `body` on the main actor. Quadratic
work that is invisible in the simulator becomes a visibly stuttering ledger on a mid-range Android-class
budget device — which is most of our market.

**The pattern to recognise:** a linear search *inside* a loop. In Swift these hide behind `contains`,
`first(where:)`, `filter`, and `firstIndex(of:)` used inside `map`, `filter`, `forEach`, or another loop.
`Array.contains` is O(n); `Set.contains` and `Dictionary` subscript are O(1).

**Incorrect (O(n × m) — every transaction rescans every card):**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/Store+Derived.swift
extension Store {
    /// Transactions made with a card that is still active.
    var activeCardTransactions: [Transaction] {
        transactions.filter { transaction in
            guard let cardID = transaction.card else { return false }
            return cards.contains { $0.id == cardID && !$0.isFrozen }   // ❌ O(cards) per transaction
        }
    }
}
// 5,000 transactions × 40 cards = 200,000 comparisons, on every `body` re-evaluation.
```

**Correct (O(n + m) — index once, then O(1) lookups):**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/Store+Derived.swift
extension Store {
    var activeCardTransactions: [Transaction] {
        let activeIDs = Set(cards.lazy.filter { !$0.isFrozen }.map(\.id))
        return transactions.filter { transaction in
            guard let cardID = transaction.card else { return false }
            return activeIDs.contains(cardID)                            // ✅ O(1)
        }
    }
}
```

**Incorrect (nested lookup to attach a card label to each row):**

```swift
let rows = transactions.map { tx in
    TransactionRow(tx: tx, cardLabel: cards.first { $0.id == tx.card }?.label)  // ❌ O(n × m)
}
```

**Correct (build a dictionary once):**

```swift
let cardsByID = Dictionary(uniqueKeysWithValues: cards.map { ($0.id, $0) })
let rows = transactions.map { tx in
    TransactionRow(tx: tx, cardLabel: tx.card.flatMap { cardsByID[$0] }?.label)
}
```

**Swift-specific traps:**
- `Dictionary(grouping:by:)` is O(n) and is the right tool for the daily grouping in `Store.grouped(_:)` —
  do not hand-roll it with nested loops.
- `array.removeFirst()` in a loop is O(n²); reverse and `popLast()`, or use an index.
- Building a string with `+=` in a loop over thousands of items reallocates; use `joined(separator:)`.
- `array.insert(_, at: 0)` per item is O(n²); append and reverse once.
- `sorted(by:)` is O(n log n) and is fine — but sorting *inside* a loop is not.
- Prefer `lazy` when chaining `filter`/`map` and only the first few results are consumed.
- `contains` on a `String` inside a loop over a large list: precompute a `Set<String>` of normalised keys.

**Always ask:** how does this behave at 10 000 transactions? If the answer requires multiplying two counts,
add an index.

Reference: [Swift Standard Library — Dictionary and Set complexity](https://developer.apple.com/documentation/swift/dictionary)
