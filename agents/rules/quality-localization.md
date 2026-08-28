---
title: Every User-Facing String Goes Through Its Package's String Catalog
impact: CRITICAL
impactDescription: Hardcoded text blocks translation; a missing `bundle: .module` silently ships the key
tags: quality, localization, i18n, strings, packages
---

## Every User-Facing String Goes Through Its Package's String Catalog

**Impact: CRITICAL**

MoniPay ships in French for Cameroon and CEMAC, with English to follow. A hardcoded literal is not just
untranslatable — it bypasses plural rules, gender agreement, and locale-aware number and date formatting.
The rule is absolute: **no user-visible text is written as a bare Swift string.**

**Source keys are English; French is a translation.** The literal you write in code is the English copy
*and* the catalog key; the `fr` entry is what a user in Douala reads. Never write a French key — an English
locale would fall back to it and ship French. See `quality-english-only-code.md`.

Formatting (money, dates, numbers), RTL-safe layout, component APIs, previews and locale-pinned tests are
covered by `quality-i18n-formatting.md`. This rule is about *where the strings live*.

### One catalog per package that owns copy

Each package that displays text owns its own
`ios/Packages/<Name>/Sources/<Name>/Resources/Localizable.xcstrings`, declared in `Package.swift`:

```swift
// ios/Packages/TopUp/Package.swift
let package = Package(
    name: "TopUp",
    defaultLocalization: "en",          // required, and it must be "en" everywhere
    platforms: [.iOS(.v26)],
    products: [.library(name: "TopUp", targets: ["TopUp"])],
    dependencies: [ /* DesignSystem, Money, WalletStore */ ],
    targets: [
        .target(
            name: "TopUp",
            dependencies: ["DesignSystem", "Money", "WalletStore"],
            resources: [.process("Resources")]
        )
    ]
)
```

`defaultLocalization` is what makes `Bundle.module` resolve a localization at all — omit it and SwiftPM
either refuses to build the target or serves the raw key. Set `en` in **every** package that ships copy,
and set the app's development language to `en` in `ios/project.yml` so the two agree.

Keys live with the code that shows them — `TopUp` copy in `TopUp`, shared component copy (empty states,
generic buttons) in `DesignSystem`. Do not centralise into one catalog: that recreates the coupling the
package split removed.

### `bundle: .module` is mandatory inside a package

This is the most common bug in this layout. In a package, `Text("Top up")` and `String(localized:)` default
to `Bundle.main` — the **app** bundle — which does not contain the package's catalog. The lookup fails
silently and the raw key ships to the user.

**Rules:**
1. In a package view: `Text("Available balance", bundle: .module)`.
2. Outside a `Text` initialiser — models, accessibility labels, alerts, error mapping — write
   `String(localized: "…", bundle: .module)` explicitly. A plain `String` there is *not* localized.
3. Add `comment:` whenever the key is ambiguous out of context ("Balance" as a noun vs. a section label).
4. Interpolate values, never concatenate: word order differs between languages.
5. Numbers, money and dates use `FormatStyle`, not `String(format:)` or `"\(n) FCFA"`.
6. Plurals go in the catalog's plural variations, never in an `if count == 1` branch in Swift. French and
   English disagree (French treats 0 as singular; other target locales have `few`/`many`), so the catalog
   is the only place that can get it right.
7. Device variations (iPhone vs. iPad wording, CarPlay, watch) also belong in the catalog's device
   variations — never in `if UIDevice.current.userInterfaceIdiom == .pad`.

**Incorrect (missing bundle, French key, hand-rolled plural, wrong number formatting):**

```swift
// ios/Packages/Home/Sources/Home/Views/BalanceCard.swift
Text("Solde disponible")                                     // ❌ French key, and Bundle.main
Text("\(balanceXAF) FCFA")                                   // ❌ no grouping, currency hardcoded
Text(cardCount == 1 ? "1 card" : "\(cardCount) cards")       // ❌ plural decided in Swift
    .accessibilityLabel("Balance " + String(balanceXAF))     // ❌ concatenated, not localized

let message = "Insufficient balance"                         // ❌ bare String: never translated
```

**Correct:**

```swift
// ios/Packages/Home/Sources/Home/Views/BalanceCard.swift
struct BalanceCard: View {
    let balance: Money
    let cardCount: Int

    private var formattedBalance: String {
        Decimal(balance.minorUnits)
            .formatted(.currency(code: balance.currency.rawValue.uppercased())
                .precision(.fractionLength(balance.currency.minorUnitDigits)))
    }

    var body: some View {
        VStack(alignment: .leading, spacing: Spacing.sm) {
            Text("Available balance", bundle: .module,
                 comment: "Title of the balance card on Home")
            Text(formattedBalance).monospacedDigit()
            // One key, plural variations declared in Localizable.xcstrings:
            //   en → one: "%lld active card" / other: "%lld active cards"
            //   fr → one: "%lld carte active" / other: "%lld cartes actives"
            Text("\(cardCount) active cards", bundle: .module)
        }
        .accessibilityElement(children: .combine)
        .accessibilityLabel(Text("Available balance: \(formattedBalance)", bundle: .module))
    }
}
```

```swift
// ios/Packages/TopUp/Sources/TopUp/Models/TopUpError+Message.swift
extension TopUpError {
    /// The only place a domain error becomes user-facing copy.
    var userMessage: String {
        switch self {
        case .amountBelowMinimum(let minimum):
            String(localized: "The minimum top-up is \(minimum.formatted()).",
                   bundle: .module,
                   comment: "Shown when the amount is below the operator minimum")
        case .operatorRefused:
            String(localized: "The operator declined this top-up.", bundle: .module)
        }
    }
}
```

The `fr` column of the catalog carries the French: *Available balance* → « Solde disponible », *Top up* →
« Recharger », *Virtual card* → « Carte virtuelle ». That mapping exists in the catalog and nowhere else.

In `ios/App/` (the composition root, not a package) `Bundle.main` is correct, so `bundle:` is omitted there.

**Review test:** grep the diff for `Text("` and `String(localized:` — every user-visible literal must be one
of them, every occurrence inside `Packages/` must carry `bundle: .module`, every key must be English, and
every new key must appear in that package's `Localizable.xcstrings` with an `fr` translation.

Reference: [Apple — Localizing and varying text with a string catalog](https://developer.apple.com/documentation/xcode/localizing-and-varying-text-with-a-string-catalog)
