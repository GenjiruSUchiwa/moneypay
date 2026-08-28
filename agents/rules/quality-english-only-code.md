---
title: Code Is English; French Lives Only in the Catalog
impact: CRITICAL
impactDescription: Mixed-language code is unsearchable, unreviewable, and hides untranslated copy
tags: quality, language, naming, localization, conventions
---

## Code Is English; French Lives Only in the Catalog

**Impact: CRITICAL**

MoniPay's users read French. MoniPay's source code does not contain a single word of it. These are two
separate concerns, and conflating them is what produces a codebase where `soldeDisponible` and
`availableBalance` are the same value, where `grep balance` misses half the call sites, and where a French
literal ships to an English user because nobody could tell copy apart from code.

**English, always:**
- Identifiers — types, properties, functions, parameters, enum cases, generic parameters.
- Comments and `///` doc comments, including `// MARK:` markers.
- Commit messages, branch names, PR titles and descriptions.
- Test names and `@Test` display names.
- Log and telemetry messages, error-enum case names, metric and event names.
- API contract fields (JSON keys, query parameters, header names) and OpenAPI summaries.
- Database table and column names, migration names, index names.
- File and folder names.

**French, only:** the *translated value* of a string in `Localizable.xcstrings` (iOS) or the server's
resource files. It is data, never source.

### The key is English; the French is a translation of it

Set `developmentLanguage: en` in `ios/project.yml` and each package's catalog. The source key is English
prose, the `fr` entry is the French the user sees. A French key looks like it works — until the English
locale falls back to it and ships French to an English user.

**Incorrect (French identifiers, French comments, French as the source key):**

```swift
// ios/Packages/TopUp/Sources/TopUp/RechargementFlow.swift   ❌ file name
struct RechargementFlow: View {                              // ❌ type
    let soldeDisponible: Money                               // ❌ property
    // On vérifie que le montant dépasse le minimum          ❌ comment
    func verifierMontant(_ montant: Int) -> Bool { … }        // ❌ function + parameter

    var body: some View {
        Text("Solde disponible", bundle: .module)             // ❌ French source key
    }
}

enum ErreurRechargement: Error { case soldeInsuffisant }      // ❌ enum + case

@Test("le rechargement refuse un montant trop faible")        // ❌ test name
func testMontantFaible() { … }
```

**Correct (English everywhere; French only as the catalog's `fr` value):**

```swift
// ios/Packages/TopUp/Sources/TopUp/TopUpFlow.swift
public struct TopUpFlow: View {
    let availableBalance: Money

    /// Rejects amounts below the operator's minimum before any network call.
    func isAmountAcceptable(_ amountXAF: Int) -> Bool { … }

    public var body: some View {
        // Key: "Available balance"  →  fr: "Solde disponible"
        Text("Available balance", bundle: .module)
    }
}

enum TopUpError: Error, Equatable, Sendable {
    case amountBelowMinimum(minimumXAF: Int)
    case insufficientBalance
}

@Test("rejects a top-up below the operator minimum")
func rejectsBelowMinimum() { … }

logger.error("top-up rejected: \(String(describing: error), privacy: .public)")
```

The catalog holds the pairing — `Available balance` → `Solde disponible`, `Top up` → `Recharger`,
`Virtual card` → `Carte virtuelle` — and nothing else does.

### Server side, same rule

**Incorrect:**

```csharp
public record DemandeRechargement(int MontantFcfa, string Operateur);  // ❌ type + members

public sealed class Rechargement                                       // ❌ → table "Rechargement"
{
    public decimal Solde { get; set; }                                 // ❌ → column "Solde"
}

return Results.BadRequest("Le montant est inférieur au minimum.");     // ❌ copy inline in code
```

**Correct:**

```csharp
public record TopUpRequest(int AmountXaf, string Operator);

public sealed class TopUp
{
    public decimal Balance { get; set; }        // column: "balance"
}

// Machine-readable code; the French text comes from a resource file keyed by it.
return Results.BadRequest(new ProblemDetails
{
    Type = "https://monipay.cm/errors/amount-below-minimum",
    Title = "Amount below minimum"
});
```

### Domain terms that have no clean English equivalent

Keep the term, spell it in ASCII, and treat it as a proper noun: `MobileMoneyOperator.mtnMoMo`,
`Currency.xaf`, `KycLevel`, `CemacRegion`. Do not invent a translation for *FCFA* or *MoMo*; do translate
*rechargement* → `topUp`, *solde* → `balance`, *carte virtuelle* → `virtualCard`, *frais* → `fee`,
*conversion* → `conversion`.

### How to check

```bash
# Accented characters outside a catalog are almost always French leaking into code.
grep -rnP '[àâçéèêëîïôûùüÿœ]' --include='*.swift' --include='*.cs' \
  ios/Packages ios/App src | grep -v 'Localizable.xcstrings'
```

Any hit is either a French comment (rewrite it) or user copy inline in code (move it to the catalog).

Reference: [Swift API Design Guidelines — Naming](https://www.swift.org/documentation/api-design-guidelines/)
