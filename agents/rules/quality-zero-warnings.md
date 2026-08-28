---
title: Zero Warnings — Fix, Never Suppress
impact: CRITICAL
impactDescription: A suppressed warning is a defect that has been made invisible
tags: quality, warnings, linting, ci, suppression
---

## Zero Warnings — Fix, Never Suppress

**Impact: CRITICAL**

A warning is the toolchain telling you it found something. The only acceptable response is to fix what it
found. Silencing it converts a visible problem into an invisible one and, worse, teaches everyone that the
warning list is noise — which is how a project ends up with 400 warnings and a compiler nobody reads.

**Both stacks build with warnings as errors.** The backend already does
(`server/Directory.Build.props`: `TreatWarningsAsErrors`, `EnableNETAnalyzers`, `EnforceCodeStyleInBuild`),
and iOS runs `swiftlint lint --strict` plus `SWIFT_TREAT_WARNINGS_AS_ERRORS`. Nothing in this repo is
allowed to weaken that.

### Forbidden

Never, in any PR, and never to "unblock" yourself:

**Swift / iOS**
- `// swiftlint:disable …` in any form — file-wide, `disable:next`, `disable:this`.
- `swiftlint lint --lenient`, dropping `--strict`, or running lint from a directory that misses files.
- Editing `ios/.swiftlint.yml` to raise a threshold, add an `excluded:` path, or move a rule to
  `disabled_rules:` so a violation stops firing.
- `SWIFT_TREAT_WARNINGS_AS_ERRORS = NO`, `GCC_TREAT_WARNINGS_AS_ERRORS = NO`, `-suppress-warnings`,
  `-Xswiftc -suppress-warnings`, or `SWIFT_SUPPRESS_WARNINGS`.
- `@unchecked Sendable` to silence a concurrency diagnostic without a proven, documented invariant.
- `try?`, `_ =`, or an empty `catch { }` used to make an "unused result" or "error not handled" warning
  disappear.
- `@available` / `#if` fences added purely to route around a deprecation instead of migrating.

**C# / backend**
- `#pragma warning disable`, `[SuppressMessage]`, `<NoWarn>`, `<WarningsNotAsErrors>`.
- Lowering `TreatWarningsAsErrors`, `AnalysisLevel`, or `EnforceCodeStyleInBuild`.
- Setting a `dotnet_diagnostic.*.severity = none`/`suggestion` in `.editorconfig` to make an existing
  violation pass, or raising a `dotnet_code_quality.*.threshold`.
- `dotnet build /p:TreatWarningsAsErrors=false`, `--no-analyzers`, or skipping
  `dotnet format --verify-no-changes`.
- `null!`, `!` (null-forgiving), or `#nullable disable` to quiet a nullability warning.

**Both**
- `HUSKY=0` outside CI to skip a pre-commit hook locally.
- `git commit --no-verify`.
- Reporting "done" on a task whose build or lint is red.

### Required

Fix the root cause. The warning names it; the fix is almost always smaller than the suppression argument.

| Diagnostic | Wrong response | Right response |
|---|---|---|
| `force_unwrapping` | `// swiftlint:disable:next` | `guard let … else { … }` |
| unused result | `_ = compute()` | use it, or mark the function `@discardableResult` if it is genuinely optional |
| `Sendable` violation | `@unchecked Sendable` | make stored properties `Sendable`, or isolate the type to an actor |
| deprecation | `@available` fence | migrate to the replacement API |
| CA1502 complexity | `#pragma warning disable CA1502` | extract functions (`quality-cyclomatic-complexity.md`) |
| CS8618 non-nullable field | `= null!` | `required` member, or a constructor parameter |
| unused `using` / `import` | leave it | delete it |

### Incorrect

```swift
// swiftlint:disable force_unwrapping cyclomatic_complexity
// Added to get CI green before the demo.

func credit(_ receipt: CollectionReceipt) async {
    let card = store.cards.first!
    try? await wallet.credit(receipt.amountXAF)     // ❌ the failure is discarded
    _ = await audit.record(receipt)                 // ❌ "unused result" silenced
}

final class RateCache: @unchecked Sendable {        // ❌ no invariant, no lock, no comment
    var rates: [String: Decimal] = [:]
}
```

```csharp
#pragma warning disable CA1502   // "this method is inherently complex"
public async Task<IResult> HandleTopUp(TopUpRequest request) { /* 40 branches */ }
#pragma warning restore CA1502

[SuppressMessage("Reliability", "CA2007")]          // ❌ blanket, no reason
public sealed class WalletService
{
    private readonly HttpClient _client = null!;    // ❌ null-forgiving to quiet CS8618
}
```

### Correct

```swift
func credit(_ receipt: CollectionReceipt) async throws(WalletError) {
    guard let card = store.cards.first else { throw .noCardSelected }
    try await wallet.credit(receipt.amountXAF, to: card.id)
    await audit.record(receipt)                     // result used, or @discardableResult upstream
}

/// Immutable snapshot: every stored property is `Sendable`, so the conformance is checked, not asserted.
struct RateCache: Sendable {
    let rates: [Currency: Decimal]
}
```

```csharp
// Split until each part is under the CA1502 threshold; no pragma needed.
public async Task<IResult> HandleTopUp(TopUpRequest request, CancellationToken cancellationToken)
{
    if (Validate(request) is { } error) return error.ToProblem();

    var receipt = await _campay.CollectAsync(request, cancellationToken);
    return await SettleAsync(receipt, request.AmountXaf, cancellationToken);
}

public sealed class WalletService(HttpClient client)   // CS8618 gone: the field is assigned
{
    private readonly HttpClient _client = client;
}
```

### The one escape hatch

A suppression is allowed only when the diagnostic is provably wrong and the fix is not ours to make — an
upstream analyzer bug, a false positive in a generated file. It must satisfy **all** of:

1. Narrowest possible scope — one line, one rule, one symbol. Never a file, never a directory.
2. A comment giving the reason **and a link to the upstream issue**.
3. A `TODO(owner, #issue)` to remove it when the upstream fix lands.
4. Its **own PR**, reviewed on its merits — never bundled into a feature PR.

```swift
// swiftlint:disable:next identifier_name
// `id` is required by Identifiable; SwiftLint flags the 2-char name.
// Upstream: https://github.com/realm/SwiftLint/issues/XXXX
// TODO(aristide, #214): remove once the rule respects protocol requirements.
public let id: CardID
```

Anything that does not meet all four is a request to fix the code instead.

### Verify before you claim done

```bash
cd ios && swiftlint lint --strict --quiet          # must exit 0, zero output
xcodebuild … build test | xcbeautify               # zero warnings, not just BUILD SUCCEEDED

dotnet build server/MoniPay.slnx -warnaserror      # zero warnings
dotnet format server/MoniPay.slnx --verify-no-changes
```

"It builds" is not the claim. "It builds with zero warnings" is. See `culture-leverage-ai.md`.

Reference: [.NET — TreatWarningsAsErrors](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-options/errors-warnings) ·
[SwiftLint — configuration](https://realm.github.io/SwiftLint/#configuration)
