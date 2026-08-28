---
title: Comments Explain Why; Doc Comments Document the Contract
impact: MEDIUM
impactDescription: Noise comments rot; missing rationale costs hours in a money flow
tags: quality, comments, documentation, docc
---

## Comments Explain Why; Doc Comments Document the Contract

**Impact: MEDIUM**

Swift is expressive enough that a comment restating the code is pure maintenance debt — it drifts, and the
next reader trusts it. Comment the *reason*: a regulatory constraint, a provider quirk, a rounding decision,
a deliberate trade-off. In a fintech codebase those reasons are the part nobody can reconstruct from the
source.

**Comment when:**
- A business or regulatory rule drives the code (BEAC/CEMAC rules, KYC thresholds, XAF having no decimals).
- A provider forces a workaround (Campay sandbox caps, Sudo Africa webhook replays, a 2 s authorisation
  window).
- A rounding or precision decision has a money consequence.
- A non-obvious performance or concurrency choice was made after measuring.
- Something looks wrong but is deliberate.

**Do not comment:** what a well-named function already says, section dividers with no content, commented-out
code (delete it — git remembers), or `// TODO` without an owner and a reason.

**Incorrect (restates the code, adds nothing):**

```swift
// Get the card
let card = store.cards.first { $0.id == id }

// Loop through transactions
for transaction in transactions {
    // Process the transaction
    process(transaction)
}

// Multiply by 100
let cents = dollars * 100
```

**Correct (explains what the code cannot):**

```swift
// Always round up to the whole XAF: the currency has no sub-unit, and rounding
// to nearest would lose the issuer ~0.5 XAF on every transaction.
var rounded = Decimal.zero
NSDecimalRound(&rounded, &raw, 0, .up)

// The card network replays authorization messages. Answer a known authId with the
// same decision, otherwise the same funds get held twice.
if let existing = holds[authId] { return .approved }

// The Campay sandbox caps collections at 100 XAF; the business minimum in
// production is 1,000 XAF. Hence the two constants.
static let minimumXAF = AppEnvironment.isLive ? 25 : 1_000
```

**Doc comments (`///`) on the package's public surface.** Anything marked `public` — a `Store` action in
`WalletStore`, a `Wallet` method, a `DesignSystem` component — gets a doc comment stating what it does, what it throws, and
any precondition. Private helpers do not need one; if a private helper needs a paragraph to explain itself,
rename or split it instead.

```swift
/// Makes the *just-in-time* funding decision for a card authorization.
///
/// Idempotent: re-presenting the same `authId` returns the original decision without
/// holding funds again. The network gives us about two seconds to answer.
///
/// - Parameters:
///   - authId: authorization identifier supplied by the issuer.
///   - amount: amount presented by the merchant, in USD cents.
/// - Returns: `.approved`, or the reason for the decline.
/// - Throws: `WalletError.cardNotFound` if the card does not belong to this wallet.
func authorize(authId: String, amount: Money) throws(WalletError) -> AuthDecision
```

Use `- Parameters`, `- Returns`, `- Throws` (DocC syntax) — Xcode Quick Help renders them, so they pay for
themselves at every call site.

Reference: [DocC — Writing symbol documentation](https://www.swift.org/documentation/docc/writing-symbol-documentation-in-your-source-files)
