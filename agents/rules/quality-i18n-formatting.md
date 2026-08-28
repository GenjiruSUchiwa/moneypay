---
title: Internationalize Formatting, Layout, and Component APIs
impact: CRITICAL
impactDescription: Locale-blind formatting and hardcoded left/right break the app outside en-US
tags: quality, i18n, formatstyle, rtl, accessibility, designsystem
---

## Internationalize Formatting, Layout, and Component APIs

**Impact: CRITICAL**

Translating strings is half of internationalization. The other half is everything around them: how a number
is grouped, how a currency is written, which direction the layout flows, and whether a component can even
*accept* a localized string. `quality-localization.md` covers where the strings live; this rule covers the
rest.

### 1. Money: `Decimal` + currency code + `FormatStyle`, never a hand-built string

`"\(amount) FCFA"` is wrong three ways: no digit grouping, the symbol is glued in English word order, and
XAF's zero decimals are not enforced. `Decimal.FormatStyle.Currency` knows all three, per locale.

**Incorrect:**

```swift
Text("\(balanceXAF) FCFA")                                   // ❌ "428500 FCFA"
Text(String(format: "$%.2f", Double(cents) / 100))           // ❌ Double money + en-US symbol
```

**Correct:**

```swift
// ios/Packages/Money/Sources/Money/Money+Format.swift
extension Money {
    /// Locale-aware currency text. XAF has 0 fraction digits, USD has 2.
    public func formatted(locale: Locale = .current) -> String {
        let major = Decimal(minorUnits) / pow(10, currency.minorUnitDigits)
        return major.formatted(
            .currency(code: currency.rawValue.uppercased())
                .precision(.fractionLength(currency.minorUnitDigits))
                .locale(locale)
        )
    }
}

// fr_CM → "428 500 F CFA"     en_US → "XAF 428,500"
Text(balance.formatted())
    .monospacedDigit()          // keeps digits from shifting as the balance updates
```

Pass `.locale(locale)` explicitly for a specific audience (a receipt, an export); default to
`Locale.current` in the UI so the device setting wins.

### 2. Dates and numbers: `FormatStyle`, which follows `Locale.current`

```swift
Text(tx.date, format: .dateTime.day().month(.abbreviated).hour().minute())
Text(usageRatio, format: .percent.precision(.fractionLength(0)))
Text(count, format: .number)                       // grouping separator per locale
Text(tx.date, format: .relative(presentation: .named))
```

Never `String(format: "%d/%m/%Y")`, and never a `DateFormatter` with a fixed `dateFormat` for display —
that is for parsing provider payloads only (see `performance-date-handling.md`).

### 3. Never concatenate; interpolate into one key

Word order is not universal, and fragments cannot be translated as a sentence.

```swift
// ❌ Three fragments a translator cannot reorder.
Text("You spent ") + Text(amount.formatted()) + Text(" this month")

// ✅ One key with a placeholder; French can move the pieces freely.
Text("You spent \(amount.formatted()) this month", bundle: .module)
```

### 4. DesignSystem components take `LocalizedStringResource` or `Text`, never `String`

A component whose label is a `String` forces every caller to resolve the string themselves, which is exactly
where `bundle: .module` gets forgotten. Accept a localizable value and let the *caller's* bundle apply.

```swift
// ❌ An already-resolved String: the caller must localize, and forgets `bundle: .module`.
public init(title: String, action: @escaping () -> Void) { … }
MPButton(title: "Recharger") { … }                       // French literal, never in a catalog
```

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/Components/Buttons/MPButton.swift
public struct MPButton: View {
    private let title: LocalizedStringResource
    private let tone: Tone
    private let action: () -> Void

    /// - Parameter title: localized in the *caller's* bundle; pass a literal from that package's catalog.
    public init(title: LocalizedStringResource, tone: Tone = .primary, action: @escaping () -> Void) { … }

    public var body: some View {
        Button(action: action) { Text(title) }
            .buttonStyle(PressStyle(tone: tone))
    }
}

// ios/Packages/TopUp/…  — key "Top up" → fr "Recharger"
MPButton(title: LocalizedStringResource("Top up", bundle: .atURL(Bundle.module.bundleURL))) { … }
```

`Text` is the other acceptable parameter type when the component only renders it. `String` is acceptable
only for genuinely non-linguistic values — an already-formatted amount, an identifier, a PAN's last four.

### 5. RTL-safe layout: leading/trailing, never left/right

Arabic support is not shipped yet, but the habits are free now and expensive to retrofit.

```swift
// ❌ Pins to the physical left, mirrors wrongly.
.padding(.left, Spacing.md)
.frame(alignment: .topLeft)
HStack { Spacer(); Text(amount) }          // fragile ordering
Image(systemName: "chevron.right")

// ✅ Direction-relative.
.padding(.leading, Spacing.md)
.frame(alignment: .topLeading)
HStack { Text(label); Spacer(); Text(amount) }
Image(systemName: "chevron.forward")       // mirrors automatically in RTL
```

Also `.multilineTextAlignment(.leading)`, and let text wrap — never `.frame(width:)` around a label. French
runs 15–30 % longer than its English source, so fixed widths clip in production and not in the simulator.

### 6. Accessibility labels are localized too

A VoiceOver label is user-facing copy. It goes through the catalog like any other string, and it should
read the *formatted* value, not the raw digits.

```swift
.accessibilityLabel(Text("Available balance: \(balance.formatted())", bundle: .module))
.accessibilityHint(Text("Opens the balance details", bundle: .module))
```

### 7. Preview every locale that ships

```swift
#Preview("Balance — fr") {
    BalanceCard(balance: .sampleXAF, cardCount: 3)
        .environment(\.locale, Locale(identifier: "fr_CM"))
}

#Preview("Balance — fr, XXL") {
    BalanceCard(balance: .sampleXAF, cardCount: 3)
        .environment(\.locale, Locale(identifier: "fr_CM"))
        .environment(\.dynamicTypeSize, .accessibility3)
}
```

Every view gets an `fr_CM` preview; add `en_US` wherever length or word order differs. Components in the
`Gallery` catalog show their variants in both locales.

### 8. Tests pin the locale — never assert against `Locale.current`

```swift
@Test("formats an XAF balance without decimals in fr_CM")
func formatsXafInFrench() {
    let balance = Money(minorUnits: 428_500, currency: .xaf)
    #expect(balance.formatted(locale: Locale(identifier: "fr_CM")) == "428 500 F CFA")
}
```

An assertion that depends on the machine's locale passes locally and fails in CI. Pin the locale (and the
time zone — see `performance-date-handling.md`) in every test that formats anything.

Reference: [Apple — Preparing views for localization](https://developer.apple.com/documentation/swiftui/preparing-views-for-localization)
