---
title: The POC Backend Contract Is the Source of Truth
impact: HIGH
impactDescription: A silently renamed JSON key becomes a nil balance on a user's home screen
tags: api, codable, dto, contract, versioning
---

## The POC Backend Contract Is the Source of Truth

**Impact: HIGH**

`poc/README.md` and the routing table in `poc/server.js` define the wire contract. The Swift client
**mirrors** it — it does not reinterpret it, does not rename its fields for elegance, and does not
invent fields the server never sends. When the contract and the app disagree, the contract wins and
the app is fixed.

**The contract, as of the POC:**

| Route | Request | Response (HTTP 200) |
|---|---|---|
| `POST /signup` | `{name, phone, email}` | `{id, name, phone, email, fcfa, card, customerId}` |
| `POST /topup` | `{user_id, amount_fcfa}` | `{tx, fcfa}` |
| `POST /simtopup` | `{user_id, amount_fcfa}` | `{fcfa}` — sandbox only |
| `POST /card` | `{user_id, amount_usd}` | `{card, debited_fcfa, fcfa}` |
| `GET /user?id=` | — | the user object |

Errors are **HTTP 400** with `{"error": "<message en clair>"}` — including business failures such as
`"Solde insuffisant: il faut 15700 F, solde 25 F"`. There is no error code; the client maps on the
route plus the HTTP status, never by parsing the French message.

**The keys mix conventions on purpose** (`amount_fcfa`, `masked_pan`, `balance_cents` in snake_case;
`customerId` in camelCase). So: **explicit `CodingKeys` on every DTO. Never
`.convertFromSnakeCase`** — a global strategy hides the mismatch until a field silently decodes to
`nil` and a balance renders as 0 F.

**Incorrect (guessed shape, lossy types, global key strategy):**

```swift
struct TopUpResponse: Decodable {
    let balance: Double        // 1) the server sends `fcfa`, not `balance`
                               // 2) Double for money: forbidden
}

let decoder = JSONDecoder()
decoder.keyDecodingStrategy = .convertFromSnakeCase   // hides every key mismatch
let response = try decoder.decode(TopUpResponse.self, from: data)
// → decoding throws — or worse, an optional field silently becomes nil.
```

**Correct (DTOs that mirror the contract, minor units, explicit keys):**

```swift
// ios/Packages/ApiClient/Sources/ApiClient/DTO.swift — one DTO per response shape in the contract.

struct SignupRequestDTO: Encodable, Sendable {
    let name: String
    let phone: String        // server format: "237670000001", no "+", no spaces
    let email: String
}

struct UserDTO: Decodable, Sendable {
    let id: String
    let name: String
    let phone: String
    let email: String
    /// Balance in FCFA. XAF has no decimals: Int, never Double.
    let fcfa: Int
    let card: CardDTO?
    let customerId: String?
}

struct CardDTO: Decodable, Sendable {
    let id: String
    let currency: String       // "USD"
    let brand: String          // "VISA"
    let maskedPan: String
    /// USD minor units (cents), as the contract specifies.
    let balanceCents: Int

    enum CodingKeys: String, CodingKey {
        case id, currency, brand
        case maskedPan = "masked_pan"
        case balanceCents = "balance_cents"
    }
}

struct TopUpRequestDTO: Encodable, Sendable {
    let userID: String
    let amountFCFA: Int
    enum CodingKeys: String, CodingKey {
        case userID = "user_id"
        case amountFCFA = "amount_fcfa"
    }
}

struct TopUpResponseDTO: Decodable, Sendable {
    let fcfa: Int
    let tx: CollectionDTO
}

struct IssueCardResponseDTO: Decodable, Sendable {
    let card: CardDTO
    let debitedFCFA: Int
    let fcfa: Int
    enum CodingKeys: String, CodingKey {
        case card, fcfa
        case debitedFCFA = "debited_fcfa"
    }
}

struct APIErrorDTO: Decodable, Sendable { let error: String }
```

**Changing the contract is a versioned act.** Adding a response field is safe; renaming or removing
one is not. The order: change `poc/server.js`, update the table in `poc/README.md` in the same
commit, then update the DTOs — never the reverse, and never a client-side shim accepting both
spellings. DTOs stay inside `ApiClient` — `internal`, never `public`, so they cannot reach a feature package or
a `View`. Map them to `Money`'s domain types at the client boundary (see `api-thin-client`).

Reference: [Encoding and decoding custom types](https://developer.apple.com/documentation/foundation/archives-and-serialization/encoding-and-decoding-custom-types)
