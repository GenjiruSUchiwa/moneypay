---
title: Test the Money, Not the Pixels
impact: HIGH
impactDescription: Every FCFA/USD path covered; view bodies deliberately excluded
tags: testing, coverage, domain, quality
---

## Test the Money, Not the Pixels

**Impact: HIGH**

Coverage targets are meaningless if they are met by asserting on view hierarchies. In MoniPay the
rule is scoped by layer: **anything that moves money, converts a currency, or decides an
authorization must be at 100 % branch coverage. SwiftUI `View` bodies are not unit-tested at all.**

Coverage is read **per package**, where the tests live (`ios/Packages/<Name>/Tests/<Name>Tests/`).

**Must be tested (no exception, no PR merged without it):**

| Package | What to assert |
|---|---|
| `Money` | Every `FXRate` conversion, rounding direction, margin application, boundary at 0 and at `Int.max / rate`; the domain models' computed properties (`maskedPan`, `usage`) |
| `WalletStore` | Each `AuthDecision` branch, hold/capture/void lifecycle, idempotent replay, decline counter → `isBlocked`; `Store` derivations (`usdEquivalentCents`, `spendByCategory`, `grouped`, `monthSpendXAF`) and every mutating action |
| `ApiClient` | Decoding of each backend response shape and each error shape, through a fake transport (see `testing-mocking`) |
| `Platform` | `Clock`, `KeyValueStoring` conformances — and `PlatformTestSupport`'s own doubles behave as documented |
| Feature packages (`TopUp`, `Cards`, …) | The `@Observable` model: state transitions idle → loading → success/failure, and what is exposed to the view after each |
| `ios/Tests/` (app target) | The composition root only: that `AppConfiguration` reads `APIBaseURL`, that `RootView` routes to the right surface |

**Must NOT be unit-tested:**
- `body` of any `View`, and the whole `DesignSystem` package (`Theme`, `CardArt`, `Viz` palettes) —
  verified visually via `#Preview` (see `testing-ui-tests`).
- `SampleData` in `WalletStore`. It is test input, not behaviour.
- Trivial `Identifiable`/`Hashable` conformances the compiler synthesises.

**Incorrect (a "test" that exercises no rule):**

```swift
@Test func cardHasLabel() {
    let store = Store()   // ios/Packages/WalletStore
    let card = store.createCard(label: "Netflix", theme: .midnight, network: .visa,
                                limitUSDCents: nil, singleUse: false)
    #expect(card.label == "Netflix")   // asserts the initialiser, not a business rule
}
```

**Correct (the money rules, including the edges that cost real FCFA):**

```swift
// ios/Packages/Money/Tests/MoneyTests/FXRateTests.swift
import Testing
@testable import Money

@Suite("FXRate — USD → FCFA conversion")
struct FXRateTests {
    let fx = FXRate(usdToXAF: 610, marginPct: 0.03)

    @Test("Applies the margin, then rounds up to the next FCFA — the fintech never loses the rounding")
    func roundsUpAfterMargin() {
        // $1.00 → 610 × 1.03 = 628.3 → 629 FCFA
        #expect(fx.xaf(fromUSDCents: 100) == 629)
    }

    @Test("Zero stays zero — no phantom fee on a nil amount")
    func zeroIsZero() { #expect(fx.xaf(fromUSDCents: 0) == 0) }

    @Test("A single cent costs at least 1 FCFA, never 0")
    func oneCentCostsAtLeastOne() { #expect(fx.xaf(fromUSDCents: 1) >= 1) }

    @Test("Monotonic: more cents can never cost fewer FCFA", arguments: 1...200)
    func monotonic(cents: Int) {
        #expect(fx.xaf(fromUSDCents: cents) <= fx.xaf(fromUSDCents: cents + 1))
    }
}

// ios/Packages/WalletStore/Tests/WalletStoreTests/WalletDeclineTests.swift
@Suite("Wallet — consecutive decline counter")
struct WalletDeclineTests {
    @Test("Three consecutive declines block the card; an approval resets the counter")
    func blocksAfterThreeDeclines() async {
        let wallet = Wallet(ownerId: "u1", balanceXAF: 1_000, maxConsecutiveDeclines: 3)
        for i in 1...3 {
            let d = await wallet.authorize(authId: "a\(i)", amountUSDCents: 100_00,
                                           spendLimitUSDCents: nil)
            #expect(d == .insufficientFunds || d == .cardBlocked)
        }
        #expect(await wallet.isBlocked)
        let after = await wallet.authorize(authId: "a4", amountUSDCents: 1,
                                           spendLimitUSDCents: nil)
        #expect(after == .cardBlocked)   // blocked short-circuits even a covered amount
    }
}
```

**Reading coverage:** per package, `swift test --package-path ios/Packages/Money
--enable-code-coverage`, then `xcrun llvm-cov report` over the emitted profdata (the path is printed
by `swift test --show-codecov-path`). Read the `Money`, `WalletStore` and `ApiClient` figures — an
app-wide percentage is diluted by view code and is not the number that matters.

**When you find a bug, the test comes first.** A regression is only fixed once a test fails without
the fix and passes with it.

Reference: [Adding tests to your Xcode project](https://developer.apple.com/documentation/xcode/adding-tests-to-your-xcode-project)
