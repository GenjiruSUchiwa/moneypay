---
title: Pin TimeZone and Locale in Tests — Never Read the Host's
impact: HIGH
impactDescription: Africa/Douala has no DST; a CI runner in UTC silently shifts every day boundary
tags: testing, timezone, locale, formatting, decimal
---

## Pin TimeZone and Locale in Tests — Never Read the Host's

**Impact: HIGH**

MoniPay ships to Cameroon: `Africa/Douala` (WAT, UTC+1, **no DST**) and `fr_CM`. Development
machines and CI runners are neither. Any code that reads `Calendar.current`, `TimeZone.current`, or
`Locale.current` produces a different answer on a laptop, on a CI runner, and on a user's phone —
and a transaction that lands on the wrong day in `Store.grouped(_:)` is a support ticket about a
missing FCFA.

Swift Testing runs suites **in parallel**, so you cannot fix this by mutating a global default in a
setup hook — another suite is reading it at the same time. **Inject the `Calendar` and the `Locale`.**

**Rules:**
- Production code that groups, formats, or compares dates takes a `Calendar` (or a `Locale`) as a
  parameter or a stored property. **`Calendar.current` never appears in `Money` or `WalletStore`** —
  SwiftLint's `custom_rules` in `ios/.swiftlint.yml` is a good place to enforce it.
- Test fixtures build dates from explicit `DateComponents` with an explicit `timeZone`, never from
  `Date()` or from a string parsed with the current locale.
- Money is formatted from `Decimal` with an explicit locale. Never assert against a hand-typed
  literal with an ordinary space — French grouping uses **U+202F narrow no-break space** (and
  U+00A0 in some styles). Build the expectation with the same formatter, or normalize before
  comparing.
- XAF has **0 decimals**, USD has 2. Assert that a XAF format style emits no fraction digits.

**Incorrect (host-dependent grouping, brittle literal comparison):**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/Store.swift — reads the ambient calendar…
func grouped(_ txs: [Transaction]) -> [(day: Date, items: [Transaction])] {
    let cal = Calendar.current   // UTC on CI, WAT on the phone → different day boundaries
    return Dictionary(grouping: txs) { cal.startOfDay(for: $0.date) } /* … */
}

@Test func groupsByDay() {
    // 00:30 in Douala = 23:30 the previous day in UTC: green on the Mac, red on CI.
    let tx = Transaction.sample(date: Date(timeIntervalSince1970: 1_800_001_800))
    #expect(store.grouped([tx]).count == 1)
}

@Test func formatsBalance() {
    #expect(Decimal(428_500).formatted(.currency(code: "XAF")) == "428 500 F CFA")  // espace ≠ U+202F
}
```

**Correct (calendar injected, dates built explicitly, formatting asserted through a shared style):**

```swift
// ios/Packages/Money/Sources/Money/Calendar+MoniPay.swift
extension Calendar {
    /// Product calendar: Douala, Cameroonian French, week starting on Monday.
    public static let moniPay: Calendar = {
        var c = Calendar(identifier: .gregorian)
        c.timeZone = TimeZone(identifier: "Africa/Douala")!
        c.locale = Locale(identifier: "fr_CM")
        c.firstWeekday = 2
        return c
    }()
}

// Store takes its calendar: production passes .moniPay, a test passes what it wants to prove.
func grouped(_ txs: [Transaction], calendar: Calendar = .moniPay)
    -> [(day: Date, items: [Transaction])] { /* … */ }

// ios/Packages/WalletStore/Tests/WalletStoreTests/TransactionGroupingTests.swift
@Suite("Store — grouping by day")
struct TransactionGroupingTests {
    let douala = TimeZone(identifier: "Africa/Douala")!

    func date(_ y: Int, _ m: Int, _ d: Int, _ h: Int, _ min: Int) -> Date {
        DateComponents(calendar: .moniPay, timeZone: douala,
                       year: y, month: m, day: d, hour: h, minute: min).date!
    }

    @Test("00:30 and 23:30 on the same Douala day form a single group")
    func sameDoualaDayIsOneGroup() throws {
        let txs = [Transaction.sample(date: date(2026, 8, 28, 0, 30)),
                   Transaction.sample(date: date(2026, 8, 28, 23, 30))]
        let groups = Store().grouped(txs, calendar: .moniPay)
        #expect(groups.count == 1)
        // The same instants grouped in UTC would split across two days — the regression we guard.
        #expect(Store().grouped(txs, calendar: {
            var c = Calendar(identifier: .gregorian); c.timeZone = .gmt; return c
        }()).count == 2)
    }
}

// ios/Packages/Money/Tests/MoneyTests/MoneyFormattingTests.swift
@Suite("Money formatting")
struct MoneyFormattingTests {
    let cm = Locale(identifier: "fr_CM")

    @Test("XAF renders no fraction digits")
    func xafHasNoFractionDigits() {
        let text = Decimal(428_500).formatted(.currency(code: "XAF").locale(cm))
        #expect(!text.contains(","))
        #expect(text.contains("428"))
        // Robust comparison: normalize the no-break spaces (U+202F, U+00A0) first.
        let normalized = text.replacingOccurrences(of: "\u{202F}", with: " ")
                             .replacingOccurrences(of: "\u{00A0}", with: " ")
        #expect(normalized.hasPrefix("428 500"))
    }

    @Test("USD renders exactly two")
    func usdHasTwoFractionDigits() {
        let text = Decimal(25).formatted(.currency(code: "USD").locale(cm))
        #expect(text.contains("25,00"))
    }
}
```

Injection is the whole answer here: `swift test --package-path ios/Packages/Money` runs with the
machine's own timezone and locale and there is no scheme to configure, so a test that depends on
ambient settings will pass locally and fail on a CI runner. `TZ=Africa/Douala swift test …` is a
diagnostic to confirm a suspicion, never the reason a test passes.

Reference: [Formatting numeric values with Decimal.FormatStyle](https://developer.apple.com/documentation/foundation/decimal/formatstyle)
