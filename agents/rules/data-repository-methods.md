---
title: Repository Method Conventions
impact: HIGH
impactDescription: Makes data access discoverable, reusable, and impossible to confuse with business logic
tags: data, repository, naming, conventions, typed-throws, pagination
---

## Repository Method Conventions

**Impact: HIGH**

A repository is a boring, predictable surface. Its methods say *what data comes back*, never *which screen
asked for it*.

Repository protocols are `public`, live in `ios/Packages/WalletStore/Sources/WalletStore/`, and follow the
`Xxxing.swift` naming convention (`CardServicing.swift`, `WalletServicing.swift`) — one protocol per file.
Implementations are actors in `ApiClient` (`data-repository-pattern.md`).

### 1. Do not repeat the entity name

The protocol already says `CardServicing`.

```swift
// ✅
public protocol CardServicing: Sendable {
    func card(id: VirtualCard.ID) async throws(CardError) -> VirtualCard
    func cards(for userID: User.ID) async throws(CardError) -> [VirtualCard]
    func issue(_ request: IssueCardRequest) async throws(CardError) -> VirtualCard
    func freeze(id: VirtualCard.ID) async throws(CardError)
}

// ❌
public protocol CardServicing: Sendable {
    func fetchCardByID(cardID: VirtualCard.ID) async throws -> VirtualCard
    func getAllCardsForUser(userID: User.ID) async throws -> [VirtualCard]
    func createNewCard(_ request: IssueCardRequest) async throws -> VirtualCard
}
```

Follow Swift naming: a non-mutating fetch reads as a noun (`cards(for:)`), an action reads as a verb
(`issue(_:)`, `freeze(id:)`). Drop `get`/`fetch` prefixes.

### 2. Every method is `async throws` with a typed error

Swift 6 typed throws make the failure surface part of the contract, so callers can switch exhaustively
instead of stringly-guessing.

```swift
enum CardError: Error, Sendable {
    case belowMinimumFunding(Money)     // Sudo requires ≥ $3
    case kycRequired
    case insufficientWalletBalance(available: Money)
    case providerRejected(code: String, message: String)
    case network
}

// Caller can be exhaustive — no `catch { }` catch-all needed.
do {
    let card = try await cards.issue(request)
} catch CardError.belowMinimumFunding(let min) {
    error = String(localized: "Minimum \(min.formatted()) to create a card.", bundle: .module)
} catch CardError.kycRequired {
    router.present(.kyc)
} catch CardError.insufficientWalletBalance(let available) {
    router.present(.topUp(suggested: request.amount - available))
} catch CardError.providerRejected(_, let message) {
    error = message   // provider text, already localized upstream
} catch CardError.network {
    error = String(localized: "Connection unavailable.", bundle: .module)
}
```

Use untyped `throws` only when a method genuinely forwards arbitrary errors (rare in a repository).

### 3. Name the extra data you load

If a method fetches relations or joins, say so, so the cheap variant stays cheap.

```swift
func card(id: VirtualCard.ID) async throws(CardError) -> VirtualCard
func cardIncludingTransactions(id: VirtualCard.ID) async throws(CardError) -> CardWithTransactions
```

Never quietly make `card(id:)` fetch 200 transactions because one screen needed them.

### 4. Generic, not use-case-specific

```swift
// ✅ describes the data
func transactions(for cardID: VirtualCard.ID, page: Page) async throws(TxError) -> PagedResult<Transaction>

// ❌ describes a screen — the next screen copies it and you have two
func transactionsForCardDetailScreen(_ id: VirtualCard.ID) async throws -> [Transaction]
```

### 5. Paginate explicitly; never return an unbounded list

```swift
struct Page: Hashable, Sendable {
    var cursor: String?
    var limit: Int = 25
}

struct PagedResult<Element: Sendable>: Sendable {
    let items: [Element]
    let nextCursor: String?
    var hasMore: Bool { nextCursor != nil }
}
```

### 6. Return domain types, never DTOs

`postTopUp` returning `TopUpDTO` puts `CodingKeys` and a provider's field names in your view model. Map at
the boundary (`data-dto-boundaries.md`) and return `TopUpTicket`.

### 7. No business logic

```swift
// ❌ Repository deciding policy — belongs in Wallet / the domain
func issue(_ request: IssueCardRequest) async throws(CardError) -> VirtualCard {
    guard await wallet.availableXAF >= fx.xaf(from: request.amount) else { throw .insufficientWalletBalance(...) }
    guard user.kycVerified else { throw .kycRequired }
    …
}

// ✅ Repository maps and calls; the caller enforced policy first
func issue(_ request: IssueCardRequest) async throws(CardError) -> VirtualCard {
    let dto: CardDTO = try await client.post("/card", IssueCardRequestDTO(request))
    return try dto.toDomain()
}
```

Reference: [Swift API Design Guidelines](https://www.swift.org/documentation/api-design-guidelines/)
