---
title: Typed Throws and Domain Error Enums
impact: CRITICAL
impactDescription: Untyped, stringly errors make failures untestable and unlocalizable
tags: quality, errors, typed-throws, localization
---

## Typed Throws and Domain Error Enums

**Impact: CRITICAL**

A money app fails in specific, enumerable ways: insufficient balance, blocked card, KYC missing, MoMo
operator timeout, issuer refusal. Model those as an `enum` and declare them with Swift's typed throws
(`throws(WalletError)`), so the compiler tells every call site exactly what it must handle. `throws` alone
erases to `any Error`, which forces `catch` blocks to guess, and `NSError(domain:"", code: -1)` or
`throw "insufficient funds"` throws away the case distinction entirely.

**Rules:**
1. One error enum per package (or per domain area within it), conforming to `Error, Equatable, Sendable`,
   and `public` when callers in other packages must switch on it.
2. Declare the concrete type: `func topUp(...) async throws(TopUpError) -> Receipt`.
3. Never throw a `String`, an `NSError` with an ad-hoc code, or a generic `AppError(message:)`.
4. Wrap foreign errors (`URLError`, `DecodingError`) into your enum with the underlying value attached.
5. Map to user-facing French text in one place — an `Error+Message.swift` in the package that owns the copy,
   using `String(localized:bundle: .module)` — not at each call site.
6. Never swallow: an empty `catch { }` or `try?` that discards a payment failure is a defect.

**Incorrect (stringly-typed, erased, and silently swallowed):**

```swift
struct AppError: Error { let message: String }

func topUp(amount: Int) async throws -> Receipt {          // throws what? nobody knows
    guard amount >= 25 else { throw AppError(message: "Amount too low") }
    do { return try await campay.collect(amount) }
    catch { throw AppError(message: error.localizedDescription) } // provider detail leaked to UI
}

// Call site
let receipt = try? await topUp(amount: 25)   // failure vanishes; the user sees a spinner forever
```

**Correct (typed throws, exhaustive handling, one message mapping):**

```swift
enum TopUpError: Error, Equatable, Sendable {
    case amountBelowMinimum(minimumXAF: Int)
    case operatorTimeout(MobileMoneyOperator)
    case operatorRefused(code: String)
    case kycRequired
    case network(URLError.Code)
}

func topUp(amountXAF: Int, from mobileOperator: MobileMoneyOperator)
    async throws(TopUpError) -> TopUpReceipt
{
    guard amountXAF >= Self.minimumXAF else {
        throw .amountBelowMinimum(minimumXAF: Self.minimumXAF)
    }
    do {
        return try await campay.collect(amountXAF: amountXAF, operator: mobileOperator)
    } catch let error as URLError {
        throw .network(error.code)
    }
}

extension TopUpError {
    /// The only place a domain error becomes user-facing copy.
    /// Keys are English; the French users read lives in Localizable.xcstrings.
    var userMessage: String {
        switch self {
        case .amountBelowMinimum(let minimum):
            String(localized: "The minimum top-up is \(minimum) XAF.", bundle: .module)
        case .operatorTimeout(let mobileOperator):
            String(localized: "\(mobileOperator.displayName) did not respond. Try again shortly.",
                   bundle: .module)
        case .operatorRefused:
            String(localized: "The operator declined this top-up.", bundle: .module)
        case .kycRequired:
            String(localized: "Verify your identity before topping up.", bundle: .module)
        case .network:
            String(localized: "No connection. Check your network.", bundle: .module)
        }
    }
}

// Call site: exhaustive, and the compiler enforces it.
do {
    receipt = try await topUp(amountXAF: amount, from: .mtnMoMo)
} catch {
    errorMessage = error.userMessage   // `error` is a TopUpError, not `any Error`
    logger.error("top-up failed: \(String(describing: error), privacy: .public)")
}
```

**Never log secrets.** PAN, CVV, MoMo tokens and API keys must be `privacy: .private` (or absent) in
`Logger` calls; error codes and enum cases are `.public`.

Reference: [SE-0413 — Typed throws](https://github.com/swiftlang/swift-evolution/blob/main/proposals/0413-typed-throws.md)
