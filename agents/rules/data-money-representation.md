---
title: Money Is Never a Double
impact: CRITICAL
impactDescription: Prevents silent rounding losses on every FCFA balance, FX conversion, and card authorization
tags: data, money, decimal, currency, xaf, usd, formatting
---

## Money Is Never a Double

**Impact: CRITICAL**

`0.1 + 0.2 != 0.3` in binary floating point. A wallet is a ledger: every amount is an exact integer count of
minor units, and every rate is a `Decimal`. MoniPay handles two currencies with different exponents — **XAF
has 0 decimals** (1 unit = 1 FCFA), **USD has 2** (1 unit = 1 cent) — so a bare `Int` is not enough either:
it must carry its currency.

These types live in the `Money` package, which depends on `Foundation` and nothing else — no SwiftUI, no
`ApiClient`. Its manifest deliberately omits `.defaultIsolation(MainActor.self)`: money code is `Sendable`
value code that must stay callable from the `Wallet` actor, from `ApiClient`, and from the main actor alike.
Everything below is `public`, because every layer above uses it.

**Incorrect (`Double` amounts, currency implied by the variable name):**

```swift
var balance: Double = 428_500.0
let usd = balance / 610.0 * 1.03            // ❌ drifts; also silently mixes rate and margin
label = String(format: "%.2f FCFA", balance) // ❌ FCFA has no decimals
totalXAF += Double(cents) / 100 * rate       // ❌ accumulating float error across a statement
```

Today's `FXRate` uses `Double` for `usdToXAF` and `marginPct`. That is tolerable only because the result is
immediately `.rounded(.up)` into an `Int`; it must not spread. New code uses `Decimal`.

**Correct (minor-unit `Int` + `Currency`, `Decimal` for rates):**

```swift
// ios/Packages/Money/Sources/Money/Money.swift
import Foundation

public enum Currency: String, Codable, Sendable, CaseIterable {
    case xaf = "XAF"      // FCFA — exponent 0
    case usd = "USD"      // cents — exponent 2

    var exponent: Int { self == .xaf ? 0 : 2 }
    var scale: Int { self == .xaf ? 1 : 100 }
}

/// An exact amount of money. Minor units only — never a Double, never a bare Int.
public struct Money: Hashable, Sendable, Codable {
    let minorUnits: Int
    let currency: Currency

    init(minorUnits: Int, currency: Currency) {
        self.minorUnits = minorUnits
        self.currency = currency
    }

    static func zero(_ currency: Currency) -> Money { Money(minorUnits: 0, currency: currency) }

    var decimalValue: Decimal {
        Decimal(minorUnits) / Decimal(currency.scale)
    }

    static func + (a: Money, b: Money) -> Money {
        precondition(a.currency == b.currency, "cannot add \(a.currency) to \(b.currency)")
        return Money(minorUnits: a.minorUnits + b.minorUnits, currency: a.currency)
    }

    static func - (a: Money, b: Money) -> Money {
        precondition(a.currency == b.currency, "cannot subtract \(b.currency) from \(a.currency)")
        return Money(minorUnits: a.minorUnits - b.minorUnits, currency: a.currency)
    }
}

extension Money: Comparable {
    static func < (a: Money, b: Money) -> Bool {
        precondition(a.currency == b.currency)
        return a.minorUnits < b.minorUnits
    }
}
```

The `precondition` on mixed currencies is the point: adding USD to XAF is a bug, and the type system now says
so. Conversion is an explicit, rate-carrying operation:

```swift
// ios/Packages/Money/Sources/Money/FXRate.swift
public struct FXRate: Sendable, Hashable {
    /// 1 USD in FCFA. Decimal, not Double.
    var usdToXAF: Decimal = 610
    /// MoniPay's FX margin — this is where the business margin lives (2–4 %), not in card fees.
    var marginPct: Decimal = 0.03

    /// Debits always round UP to the FCFA: the fintech never loses on rounding.
    func xaf(from usd: Money) -> Money {
        precondition(usd.currency == .usd)
        let raw = usd.decimalValue * usdToXAF * (1 + marginPct)
        return Money(minorUnits: raw.minorUnits(scale: 0, rule: .up), currency: .xaf)
    }

    /// Credits round DOWN, for the same reason. Never reuse one helper for both directions.
    func usd(from xaf: Money) -> Money {
        precondition(xaf.currency == .xaf)
        let raw = xaf.decimalValue / (usdToXAF * (1 + marginPct)) * 100
        return Money(minorUnits: raw.minorUnits(scale: 0, rule: .down), currency: .usd)
    }
}

private extension Decimal {
    /// Exact decimal rounding. `Decimal` has no `rounded(_:)`; go through NSDecimalRound
    /// rather than converting to Double at any point.
    func minorUnits(scale: Int, rule: NSDecimalNumber.RoundingMode) -> Int {
        var input = self
        var output = Decimal()
        NSDecimalRound(&output, &input, scale, rule)
        return NSDecimalNumber(decimal: output).intValue
    }
}
```

**Rounding is a policy, never an accident.** State the direction at every conversion (`.up` on debits,
`.down` on credits) and keep the rate that was applied on the transaction record, so a receipt can be
reproduced months later:

```swift
struct Transaction: Identifiable, Hashable, Sendable {
    let id: UUID
    var presented: Money      // what the merchant charged, USD
    var settled: Money        // what moved on the wallet, XAF
    var appliedRate: FXRate   // the rate + margin at authorization time
}
```

**Formatting: `FormatStyle`, never `String(format:)`.** The UI is French; XAF must show no decimals.

```swift
extension Money {
    /// "428 500 FCFA" / "10,99 $US" with French grouping and the right fraction digits.
    func formatted(locale: Locale = Locale(identifier: "fr_CM")) -> String {
        decimalValue.formatted(
            .currency(code: currency.rawValue)
             .precision(.fractionLength(currency.exponent))
             .locale(locale)
        )
    }

    /// Compact digits for the keypad header, no currency symbol.
    var digitsOnly: String {
        decimalValue.formatted(.number.precision(.fractionLength(currency.exponent)).grouping(.automatic))
    }
}
```

**Checklist**
- No `Double`, `Float`, or `CGFloat` ever holds an amount. `Viz` percentages and chart ratios may.
- No amount is stored without its `Currency`.
- Every conversion states its rounding direction and persists the rate used.
- Formatting goes through `FormatStyle`; XAF renders 0 fraction digits, USD renders 2.
- Comparisons and arithmetic across currencies are impossible by construction.

Reference: [Foundation Decimal](https://developer.apple.com/documentation/foundation/decimal) ·
[FormatStyle currency](https://developer.apple.com/documentation/foundation/formatstyle/currency)
