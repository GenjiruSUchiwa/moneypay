---
title: Make Illegal States Unrepresentable
impact: CRITICAL
impactDescription: Stringly-typed money and IDs are the highest-risk defect class in a wallet app
tags: quality, types, enums, newtype, money
---

## Make Illegal States Unrepresentable

**Impact: CRITICAL**

Swift's type system is the cheapest test suite available. Use it: no `Any`, no `[String: Any]` payloads
carried into the app layer, no raw `String` for things that are not prose, no `Bool` pairs that can both be
true. If a bug can be made impossible at compile time, it must be.

**1. No `Any`, no untyped dictionaries past the network boundary.** Decode into a `Codable` struct at the
edge; the rest of the app never sees a dictionary.

**2. No stringly-typed domain values.** Statuses, operators, currencies and card networks are `enum`s. A
`String` status invites `status == "aproved"` to compile and silently never match.

**3. Enums with associated values instead of parallel optionals.** If two properties are only ever valid
together, put them in the same case.

**4. Newtype IDs.** `UUID` for a card and `UUID` for a transaction are the same type to the compiler; wrap
them so they cannot be swapped at a call site.

**Incorrect (every illegal state is representable):**

```swift
struct Transaction {
    var status: String            // "approved"? "Approved"? "APPROVE"?
    var amount: Double            // money as Double — 0.1 + 0.2 != 0.3
    var currency: String          // "XAF", "FCFA", "xaf"
    var cardID: UUID?
    var declineReason: String?    // set only when declined — but nothing enforces it
    var raw: [String: Any]        // the provider payload, dragged through the app
}

// Compiles, always false, ships:
if transaction.status == "Approved" { credit(walletID: transaction.cardID) }
```

**Correct (the compiler rejects the nonsense):**

```swift
// ios/Packages/Money/Sources/Money/Identifiers.swift
public struct CardID: Hashable, Sendable, Codable { public let rawValue: UUID }
public struct TransactionID: Hashable, Sendable, Codable { public let rawValue: UUID }

enum Currency: String, Sendable, Codable {
    case xaf, usd
    /// XAF has no decimals; USD has two.
    var minorUnitDigits: Int { self == .xaf ? 0 : 2 }
}

/// Minor units plus currency. Never a Double for money.
struct Money: Hashable, Sendable, Codable {
    let minorUnits: Int
    let currency: Currency
}

enum TransactionOutcome: Sendable, Equatable {
    case approved(capturedAt: Date)
    case pending
    case declined(reason: DeclineReason)   // the reason exists ONLY when declined
    case refunded(originalID: TransactionID)
}

struct Transaction: Identifiable, Sendable {
    let id: TransactionID
    let card: CardID?                      // nil = wallet movement (a top-up)
    let presented: Money
    let settled: Money
    let outcome: TransactionOutcome
}

// `switch` is exhaustive: adding a case breaks the build instead of the app.
switch transaction.outcome {
case .approved(let date):     showReceipt(at: date)
case .pending:                showPendingBadge()
case .declined(let reason):   showDecline(reason.userMessage)
case .refunded(let original): showRefund(of: original)
}

// credit(walletID: transaction.card)  // ❌ does not compile: a CardID is not a WalletID
```

**Money is never `Double`, never `Float`.** Use `Decimal` for rates and computed amounts, and `Int` minor
units for stored balances. `Double(cents) / 100 * rate` accumulates error and, over a ledger, the FCFA
balance stops matching the sum of its transactions.

Reference: [Foundation — Decimal](https://developer.apple.com/documentation/foundation/decimal)
