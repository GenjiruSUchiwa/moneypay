---
title: The API Client Does Transport and Decoding — Nothing Else
impact: HIGH
impactDescription: Business rules duplicated in the client drift from the ledger
tags: api, networking, error-handling, typed-throws, architecture
---

## The API Client Does Transport and Decoding — Nothing Else

**Impact: HIGH**

`MoniPayAPIClient`, in `ios/Packages/ApiClient/`, builds a `URLRequest`, sends it, decodes the DTO,
and maps failures to a typed domain error. That is the whole job — and it is why `ApiClient` sits
below every feature package and depends only on `Money`. It does **not** decide whether a balance is sufficient, does
not compute the FCFA cost of a card, does not apply the FX margin, does not format anything for
display, and does not touch `Store`.

Why this is not pedantry: the server is the ledger. If the client also computes "$25 costs
15,700 FCFA" and the server's rate moves, the user sees one number and is debited another. The
client asks, and reports faithfully what came back — including `debited_fcfa`.

**Only these:** build the request (URL, method, headers, `Encodable` DTO body); send it via
`URLSession`; check the HTTP status; decode the success DTO or `{"error": …}` on 400; map failures
onto a typed `MoniPayAPIError`; map DTO → `Money` domain type at the boundary. **Never:** validate
business rules, retry silently, cache, log PII (a full PAN, a phone number), format currency, or
mutate app state.

**Incorrect (business logic and UI concerns inside the client):**

```swift
func issueCard(userID: String, amountUSD: Decimal) async throws -> VirtualCard {
    let costFCFA = Int(truncating: (amountUSD * 628) as NSDecimalNumber)  // 1) rate duplicated
    guard store.balanceXAF >= costFCFA else {                             // 2) reads app state
        throw NSError(domain: "", code: 0,
                      userInfo: [NSLocalizedDescriptionKey: "Insufficient balance"])  // 3) UI copy
    }
    let (data, _) = try await URLSession.shared.data(for: request)  // 4) HTTP status ignored
    store.balanceXAF -= costFCFA                                    // 5) mutates app state
    return try JSONDecoder().decode(VirtualCard.self, from: data)   // 6) domain decoded from wire
}
```

**Correct (transport + decoding + typed error mapping):**

```swift
// ios/Packages/ApiClient/Sources/ApiClient/MoniPayAPIClient.swift
public enum MoniPayAPIError: Error, Sendable, Equatable {
    case offline
    case timedOut
    case rejected(message: String)      // HTTP 400 + {"error": …} — the server decided
    case unexpectedStatus(Int)
    case malformedResponse
}

public actor MoniPayAPIClient: CardIssuing, TopUpService {
    private let baseURL: URL
    private let session: URLSession
    private let decoder = JSONDecoder()
    private let encoder = JSONEncoder()

    /// baseURL comes from Config/*.xcconfig via AppConfiguration; never hard-coded here.
    public init(baseURL: URL, session: URLSession = .shared) {
        self.baseURL = baseURL
        self.session = session
    }

    public func issueCard(userID: String, amountUSD: Decimal)
        async throws(MoniPayAPIError) -> IssuedCard
    {
        let dto: IssueCardResponseDTO = try await post(
            "card",
            body: IssueCardRequestDTO(userID: userID, amountUSD: amountUSD)
        )
        // The only permitted transform: DTO → domain type. The cost comes from the server.
        return IssuedCard(card: dto.card.toDomain(),
                          debitedXAF: dto.debitedFCFA,
                          balanceXAF: dto.fcfa)
    }

    // MARK: - Transport

    private func post<Body: Encodable & Sendable, Response: Decodable & Sendable>(
        _ path: String, body: Body
    ) async throws(MoniPayAPIError) -> Response {
        var request = URLRequest(url: baseURL.appending(path: path))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.timeoutInterval = 30

        let data: Data
        let response: URLResponse
        do {
            request.httpBody = try encoder.encode(body)
            (data, response) = try await session.data(for: request)
        } catch let error as URLError {
            let mapped: MoniPayAPIError = switch error.code {
            case .notConnectedToInternet, .networkConnectionLost: .offline
            case .timedOut: .timedOut
            default: .unexpectedStatus(error.errorCode)
            }
            throw mapped
        } catch {
            throw .malformedResponse
        }

        guard let http = response as? HTTPURLResponse else { throw .malformedResponse }
        switch http.statusCode {
        case 200:
            guard let decoded = try? decoder.decode(Response.self, from: data) else {
                throw .malformedResponse
            }
            return decoded
        case 400:
            // The server message is surfaced verbatim; the presentation layer translates it.
            let message = (try? decoder.decode(APIErrorDTO.self, from: data))?.error ?? ""
            throw .rejected(message: message)
        default:
            throw .unexpectedStatus(http.statusCode)
        }
    }
}
```

The feature package's `@Observable` model turns `MoniPayAPIError` into French copy; the `View`
renders it. Three layers, one responsibility each — and the model depends on the `CardIssuing`
protocol, never on the concrete `MoniPayAPIClient`, which is what lets it take
`ApiClientTestSupport`'s stub in tests (see `testing-mocking`).

Reference: [Typed throws (SE-0413)](https://github.com/swiftlang/swift-evolution/blob/main/proposals/0413-typed-throws.md)
