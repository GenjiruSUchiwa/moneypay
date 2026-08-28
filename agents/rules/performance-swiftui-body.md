---
title: Keep SwiftUI `body` Cheap
impact: HIGH
impactDescription: `body` runs on every dependency change — expensive bodies drop frames
tags: performance, swiftui, observable, lazy, identity
---

## Keep SwiftUI `body` Cheap

**Impact: HIGH**

`body` is a pure function SwiftUI may evaluate many times per second, on the main actor, for every visible
view. Anything expensive inside it — sorting the ledger, formatting currency, date arithmetic, computing
category totals — is paid again on each keystroke, each scroll tick, each balance update. The rule: `body`
composes views from values that are *already computed*.

### 1. Move computation out of `body`

**Incorrect (sorts, groups, and formats the whole ledger on every render):**

```swift
// ios/Packages/Transactions/Sources/Transactions/TransactionsView.swift
struct TransactionsView: View {
    @Environment(Store.self) private var store
    @State private var query = ""

    var body: some View {
        List {
            // ❌ filter + sort + Dictionary(grouping:) + formatter, on every keystroke
            ForEach(Dictionary(grouping: store.transactions.filter {
                $0.merchant.lowercased().contains(query.lowercased())
            }, by: { Calendar.current.startOfDay(for: $0.date) })
                .sorted { $0.key > $1.key }, id: \.key) { day, items in
                Section(day.formatted(date: .abbreviated, time: .omitted)) {
                    ForEach(items) { TransactionRow(tx: $0) }
                }
            }
        }
    }
}
```

**Correct (the model owns the derivation; `body` only lays out):**

```swift
// ios/Packages/Transactions/Sources/Transactions/TransactionsModel.swift
@Observable
final class TransactionsModel {
    var query = "" { didSet { rebuild() } }
    private(set) var sections: [DaySection] = []

    private let all: [Transaction]

    private func rebuild() {
        let needle = query.lowercased()
        let matches = needle.isEmpty ? all
                                     : all.filter { $0.merchant.lowercased().contains(needle) }
        sections = DaySection.group(matches)   // computed once per query, not per render
    }
}

struct TransactionsView: View {
    @State private var model: TransactionsModel

    var body: some View {
        List {
            ForEach(model.sections) { section in
                Section(section.title) {
                    ForEach(section.items) { TransactionRow(tx: $0) }
                }
            }
        }
        .searchable(text: $model.query)
    }
}
```

If the derivation is genuinely heavy, do it off the main actor and assign the result back on `@MainActor`.

### 2. Never do I/O in `body`

No network calls, no `FileManager`, no `JSONDecoder`, no image decoding. Use `.task { }` — SwiftUI cancels
it on disappear — or `.task(id:)` when the work depends on a value.

### 3. Lazy containers and stable identities

```swift
// ❌ VStack in a ScrollView builds every row up front, even off-screen.
ScrollView { VStack { ForEach(transactions) { TransactionRow(tx: $0) } } }

// ✅ LazyVStack (or List) builds only what is visible.
ScrollView { LazyVStack(spacing: 12) { ForEach(transactions) { TransactionRow(tx: $0) } } }
```

`ForEach` identity must be stable and unique. `Transaction` and `VirtualCard` are `Identifiable` with a
`UUID` — use that. Never `id: \.self` on a mutable struct (any field change destroys and rebuilds the row,
losing scroll position and animations) and never array indices for a list that can reorder.

### 4. `@Observable` tracks per-property — keep views narrow

`@Observable` invalidates only views that read the properties that changed, so pass a subview the smallest
value it needs instead of the whole `Store` (which lives in `WalletStore` and every feature can reach):

```swift
// ❌ CardRow reads `store`, so it re-renders when an unrelated notification arrives.
struct CardRow: View { @Environment(Store.self) var store; let id: CardID }

// ✅ CardRow depends only on the card it draws.
struct CardRow: View { let card: VirtualCard }
```

### 5. Equatable views for expensive subtrees

When a subview is costly to build and its inputs rarely change, conform it to `Equatable` and apply
`.equatable()` so SwiftUI can skip re-evaluating it:

```swift
struct SpendChart: View, Equatable {
    let points: [CategorySpend]
    static func == (a: Self, b: Self) -> Bool { a.points == b.points }
    var body: some View { /* Swift Charts */ }
}

SpendChart(points: model.spendByCategory).equatable()
```

### 6. Liquid Glass

Group glass elements in one `GlassEffectContainer` instead of applying `.glassEffect()` to many separate
leaves — the container blends and rasterises once rather than per element.

Reference: [Apple — Demystify SwiftUI performance (WWDC23)](https://developer.apple.com/videos/play/wwdc2023/10160/)
