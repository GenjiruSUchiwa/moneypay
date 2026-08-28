---
title: Prefer the Simplest Swift That Solves Today's Problem
impact: CRITICAL
impactDescription: Speculative abstractions are the main source of dead code and slow reviews
tags: quality, simplicity, yagni, value-types
---

## Prefer the Simplest Swift That Solves Today's Problem

**Impact: CRITICAL**

Swift makes abstraction cheap: protocols, generics, property wrappers, result builders. That is exactly why
a codebase accumulates them faster than it needs them. Every protocol with a single conformer, every generic
parameter with a single instantiation, and every "provider" indirection is code a reader must hold in their
head before they can answer "what actually debits the wallet?".

**Questions to ask before adding an abstraction:**
- Does a second conformer / second implementation exist *today*, in this PR?
- Am I adding this only so a test can substitute a fake? If yes, say so explicitly in the PR description.
- Can this be a `struct` instead of a `class`? A `func` instead of a type? An `enum` instead of a protocol?
- Have I considered one concrete alternative, and can I say why this one is better?

**Incorrect (protocol + generic + factory for exactly one implementation):**

```swift
protocol FXRateProviding: Sendable {
    func currentRate() async throws -> FXRate
}

struct LiveFXRateProvider: FXRateProviding {
    func currentRate() async throws -> FXRate { FXRate() }
}

enum FXRateProviderFactory {
    static func make() -> any FXRateProviding { LiveFXRateProvider() }
}

// One conformer, one call site, three types, an existential box, and a factory
// that only ever returns the same value. The reader pays for flexibility nobody uses.
@Observable final class ConvertModel<Provider: FXRateProviding> { /* ... */ }
```

**Correct (one concrete value type; abstract when the second case actually arrives):**

```swift
struct FXRate: Sendable {
    var usdToXAF: Decimal
    var marginPct: Decimal

    /// Round up to the whole XAF: the currency has no decimals.
    func xaf(fromUSDCents cents: Int) -> Int {
        let usd = Decimal(cents) / 100
        var raw = usd * usdToXAF * (1 + marginPct)
        var rounded = Decimal.zero
        NSDecimalRound(&rounded, &raw, 0, .up)
        return NSDecimalNumber(decimal: rounded).intValue
    }
}
```

**Prefer value types.** `struct` and `enum` are the default in this codebase: everything in the `Money`
package — `VirtualCard`, `Transaction`, `FXRate` — is a value. Reach for a reference type only when you need identity or shared mutable state
— `@Observable final class Store` and `actor Wallet`, both in `WalletStore`, are the two justified cases
today. A `class` with only
`let` properties and no identity is a `struct` you have not written yet.

**Simple is not anemic.** Ship the whole feature: the empty state, the error state, the FCFA formatting,
the VoiceOver label. Simplicity is about the *shape* of the code, never about cutting user-visible behaviour.

Reference: [Swift API Design Guidelines](https://www.swift.org/documentation/api-design-guidelines/)
