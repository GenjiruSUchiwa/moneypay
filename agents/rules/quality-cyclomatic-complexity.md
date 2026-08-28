---
title: Keep Cyclomatic Complexity Under the Threshold
impact: HIGH
impactDescription: Every extra branch doubles the paths a reviewer and a test suite must cover
tags: quality, complexity, swiftlint, roslyn, refactoring
---

## Keep Cyclomatic Complexity Under the Threshold

**Impact: HIGH**

Cyclomatic complexity counts the independent paths through a function: one, plus one for each decision
point. It is the closest thing we have to a mechanical measure of "how hard is this to reason about", and
in a wallet app it is also the number of paths a test suite has to cover before a money bug can be ruled
out. A function at complexity 20 has 20 paths; nobody tests 20 paths, so most of them ship unverified.

Both stacks enforce a threshold, the same way `eslint`/`oxc` enforce `complexity` on the web.

### What counts as a decision point

`if`, `else if`, `guard`, each `case` in a `switch`, each loop (`for`, `while`, `repeat`), each `&&` and
`||` in a condition, each ternary `?:`, each `catch` clause, and each optional-chain or nil-coalescing
short-circuit (`?.`, `??`, `try?`). C# adds `when` clauses, `is` pattern arms, and `?.`/`??`/`??=`.

`else` alone does not add a path; the `if` already counted it.

### Swift — SwiftLint

`ios/.swiftlint.yml` sets the gate. Target values:

```yaml
cyclomatic_complexity:
  warning: 10
  error: 15
  ignores_case_statements: false   # a wide switch is still a wide switch

function_body_length:
  warning: 50
  error: 80

# closure_body_length is not enabled: SwiftUI @ViewBuilder closures are markup, not logic.

nesting:
  type_level: 2
  function_level: 3
```

`--strict` makes the warning fail the build, so 10 is the real ceiling. A `switch` over a closed enum is
the one case where a high count is often fine — but prove it by keeping the *arms* trivial (one expression
each); if an arm has its own branching, extract it.

### C# — Roslyn analyzers

Enable CA1502 in the repo's `.editorconfig` so the backend has the same gate:

```ini
[*.cs]
dotnet_diagnostic.CA1502.severity = warning     # Avoid excessive complexity
dotnet_code_quality.CA1502.threshold = 10

dotnet_diagnostic.CA1505.severity = suggestion  # Avoid unmaintainable code
dotnet_diagnostic.CA1506.severity = suggestion  # Avoid excessive class coupling
dotnet_code_quality.CA1506.threshold = 20
```

Because the backend builds with `-warnaserror` (see `quality-pr-creation.md`), CA1502 at `warning` is a
hard gate. CA1505 (maintainability index) and CA1506 (class coupling) stay advisory: they flag the
*type*-level version of the same problem — a class doing too much — and are worth reading before a
refactor rather than blocking a PR.

### Incorrect — a top-up state machine at complexity ~19

```swift
func handleTopUp(_ request: TopUpRequest) async -> TopUpOutcome {
    if request.amountXAF > 0 {
        if user.isKycVerified {
            if request.amountXAF >= minimumXAF && request.amountXAF <= maximumXAF {
                if operatorIsAvailable(request.mobileOperator) {
                    if let phone = request.phone, phone.hasPrefix("+237") {
                        do {
                            let receipt = try await campay.collect(request)
                            if receipt.status == .settled {
                                if receipt.amountXAF == request.amountXAF {
                                    await wallet.credit(receipt.amountXAF)
                                    return .credited(receipt)
                                } else {
                                    await wallet.credit(receipt.amountXAF)
                                    return .creditedPartially(receipt)
                                }
                            } else if receipt.status == .pending {
                                return .pending(receipt.reference)
                            } else {
                                return .failed(.operatorRefused(code: receipt.code ?? "unknown"))
                            }
                        } catch { return .failed(.network(.unknown)) }
                    } else { return .failed(.invalidPhone) }
                } else { return .failed(.operatorTimeout(request.mobileOperator)) }
            } else { return .failed(.amountBelowMinimum(minimumXAF: minimumXAF)) }
        } else { return .failed(.kycRequired) }
    } else { return .failed(.invalidAmount) }
}
```

Six levels of nesting, the happy path buried at the bottom, and the validation rules tangled with the
network call and the ledger write.

### Correct — validate, then dispatch over an enum

```swift
/// Validation only: no I/O, complexity 5, trivially testable.
private func validate(_ request: TopUpRequest) -> TopUpError? {
    guard request.amountXAF > 0 else { return .invalidAmount }
    guard user.isKycVerified else { return .kycRequired }
    guard (minimumXAF...maximumXAF).contains(request.amountXAF) else {
        return .amountBelowMinimum(minimumXAF: minimumXAF)
    }
    guard operatorIsAvailable(request.mobileOperator) else {
        return .operatorTimeout(request.mobileOperator)
    }
    guard request.phone?.hasPrefix("+237") == true else { return .invalidPhone }
    return nil
}

/// Settlement only: one switch, one trivial arm each. Complexity 4.
private func settle(_ receipt: CollectionReceipt, requested: Int) async -> TopUpOutcome {
    switch receipt.status {
    case .settled where receipt.amountXAF == requested:
        await wallet.credit(receipt.amountXAF)
        return .credited(receipt)
    case .settled:
        await wallet.credit(receipt.amountXAF)
        return .creditedPartially(receipt)
    case .pending:
        return .pending(receipt.reference)
    case .refused:
        return .failed(.operatorRefused(code: receipt.code ?? "unknown"))
    }
}

/// Orchestration only. Complexity 3, and the happy path reads top to bottom.
func handleTopUp(_ request: TopUpRequest) async -> TopUpOutcome {
    if let error = validate(request) { return .failed(error) }
    do {
        let receipt = try await campay.collect(request)
        return await settle(receipt, requested: request.amountXAF)
    } catch {
        return .failed(.network(.unknown))
    }
}
```

### How to bring a function back under the line

1. **Early return / `guard`.** Invert conditions so failures leave immediately and the happy path stays at
   indent level one. This alone fixes most violations.
2. **Extract a named function** per cohesive block. The name replaces the comment you were about to write.
3. **Dispatch over an enum** instead of an `if`/`else if` chain on a `String` or a `Bool` pair. A `switch`
   over a closed enum is exhaustive, so adding a case breaks the build instead of the app.
4. **Table lookup** for a mapping: a `Dictionary` or a computed property on the enum, not ten branches.
5. **Collapse boolean algebra** into a named predicate — `card.canAuthorize` beats
   `!card.isFrozen && card.spent < limit && card.onlineAllowed`.
6. **Strategy via a protocol** — but only when a second implementation genuinely exists today; see
   `quality-simplicity.md`.

### Never raise the threshold to pass

`// swiftlint:disable:next cyclomatic_complexity` and `#pragma warning disable CA1502` need a written
reason, reviewed like any other suppression (see `quality-swiftlint.md`). "This function is inherently
complex" is not one — a generated parser or an exhaustive `switch` over a 20-case protocol enum might be.

The `cyclomatic-complexity` skill is available to agents: invoke it to get a guided refactor of a flagged
function rather than reshuffling branches by hand.

Reference: [SwiftLint — cyclomatic_complexity](https://realm.github.io/SwiftLint/cyclomatic_complexity.html) ·
[CA1502: Avoid excessive complexity](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502)
