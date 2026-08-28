---
title: Map DTOs to Domain Models at the Boundary
impact: HIGH
impactDescription: Stops provider JSON from dictating the app's types and leaking the full PAN into the UI
tags: data, dto, codable, boundaries, security, mapping
---

## Map DTOs to Domain Models at the Boundary

**Impact: HIGH**

Sudo Africa returns `{"statusCode":400}` inside an HTTP 200. Campay names a field `reference` in one endpoint
and `external_reference` in another. `account._id` in a `/cards` response is not the `debitAccountId` we sent.
None of that may reach a SwiftUI view.

`Codable` DTOs live in `ApiClient`, mirror the wire exactly, and are converted to `Money` package models in one
place. Views and view models see only domain types.

**Incorrect (wire types used as app types):**

```swift
// ios/Packages/ApiClient/Sources/ApiClient/DTO/CardDTO.swift
struct CardDTO: Codable {
    let _id: String
    let maskedPan: String
    let pan: String?             // full PAN — must never travel further
    let cvv2: String?
    let expiryMonth: String
    let expiryYear: String
    let status: String           // "active" | "canceled" | whatever ships next quarter
}

// ios/Packages/Cards/Sources/Cards/Views/CardsListView.swift
struct CardRow: View {
    let card: CardDTO                              // ❌ view coupled to Sudo's schema
    var body: some View {
        Text(card.maskedPan)
        Text(card.status == "active" ? "Active" : "Frozen")  // ❌ stringly-typed business rule in UI
    }
}
```

The full PAN and CVV are now one `print()` or one `.sheet` away from the screen; renaming a provider field
breaks views; a new `status` value silently renders as "Gelée".

**Correct (DTO → domain at the boundary, secrets dropped in transit):**

```swift
// ios/Packages/ApiClient/Sources/ApiClient/DTO/CardDTO.swift — mirrors the wire, NOT public
struct CardDTO: Decodable {
    let id: String
    let maskedPan: String
    let expiryMonth: Int
    let expiryYear: Int
    let status: String
    let brand: String
    let spendingLimitCents: Int?

    enum CodingKeys: String, CodingKey {
        case id = "_id", maskedPan, expiryMonth, expiryYear, status, brand
        case spendingLimitCents = "spendingLimit"
    }
}

extension CardDTO {
    func toDomain() throws(DecodingFailure) -> VirtualCard {
        guard let uuid = UUID(uuidString: id) else { throw .badIdentifier(id) }
        return VirtualCard(
            id: uuid,
            last4: String(maskedPan.suffix(4)),
            network: CardNetwork(wire: brand) ?? .mastercard,
            expiry: CardExpiry(month: expiryMonth, year: expiryYear),
            isFrozen: status != "active",
            monthlyLimit: spendingLimitCents.map { Money(minorUnits: $0, currency: .usd) }
        )
    }
}
```

```swift
// ios/Packages/Money/Sources/Money/VirtualCard.swift — what the rest of the app sees
struct VirtualCard: Identifiable, Hashable, Sendable {
    let id: UUID
    let last4: String                 // never the full PAN
    var network: CardNetwork
    var expiry: CardExpiry
    var isFrozen: Bool
    var monthlyLimit: Money?

    var maskedPan: String { "•••• •••• •••• \(last4)" }
}
```

**The standard**

1. **DTOs live in `ios/Packages/ApiClient/Sources/ApiClient/DTO/` and are never `public`.** `internal`
   is the boundary: no other package can name a DTO even by accident. They are `Decodable`
   (or `Encodable`) and nothing else — no computed business properties, no `View` conformances.
2. **Mapping is explicit and fallible.** `toDomain()` returns a domain type or throws; never `as!`, never a
   force-unwrap, never a silent default that hides a schema change.
3. **Enums cross the boundary as closed Swift enums**, with an unknown case handled explicitly:
   ```swift
   enum CardNetwork: String, Sendable { case visa, mastercard
       init?(wire: String) { self.init(rawValue: wire.lowercased()) }
   }
   ```
   A provider adding `"verve"` must produce a decode failure you can see, not a wrong card art.
4. **Sensitive fields are dropped at the boundary, not later.** Full PAN and CVV are decoded only inside the
   one-shot "reveal" call, held in a `@State` for the duration of the sheet, and never stored in a model,
   a cache, or a log. `VirtualCard` has no `pan` property at all.
5. **Outbound requests get their own DTOs.** Do not make a domain model `Encodable` to save typing — that
   pins the domain to a provider's field names for ever.
6. **Money never round-trips through `Double`.** `Money(minorUnits: dto.balanceFcfa, currency: .xaf)`, never
   `Money(dto.balance)` where `balance` is a JSON float (`data-money-representation.md`).

Yes, this is more code than decoding straight into your models. It is the reason a provider swap is a
one-file change.

Reference: [Encoding and Decoding Custom Types](https://developer.apple.com/documentation/foundation/archives-and-serialization/encoding-and-decoding-custom-types)
