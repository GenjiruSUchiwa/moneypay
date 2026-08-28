---
title: Isolate Providers Behind Protocol Repositories
impact: HIGH
impactDescription: Swapping Campay, Sudo Africa, or the mock server touches one file instead of the whole app
tags: data, repository, protocols, actors, testing
---

## Isolate Providers Behind Protocol Repositories

**Impact: HIGH**

MoniPay talks to Campay (MoMo collection), Sudo Africa (USD card issuing), and our own POC backend. Those
choices are not settled — Maplerad and Bridgecard were dropped mid-POC, Notch Pay is the accepted plan B. If
provider names, DTOs, or `URLSession` calls appear in views and view models, every provider change becomes a
codebase-wide refactor.

The contract is a **`public` protocol in `WalletStore`** (file `WalletServicing.swift`, per the `Xxxing.swift`
naming convention). The implementation is an **actor in `ApiClient`**. Only `ios/App/` ever names the
implementation, so no feature package knows a provider exists.

**Incorrect (provider leaking into a feature):**

```swift
// ios/Packages/TopUp/Sources/TopUp/Flows/TopUpFlow.swift
struct TopUpFlow: View {
    func confirm() async {
        var request = URLRequest(url: URL(string: "https://demo.campay.net/api/collect/")!)
        request.httpMethod = "POST"
        request.setValue("Token \(Secrets.campayToken)", forHTTPHeaderField: "Authorization")
        let (data, _) = try await URLSession.shared.data(for: request)   // ❌ HTTP in a view
        let json = try JSONSerialization.jsonObject(with: data)          // ❌ untyped
        store.balanceXAF += amount                                       // ❌ optimistic, unverified
    }
}
```

**Correct (protocol contract, actor implementation, feature sees neither):**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/WalletServicing.swift
protocol WalletServicing: Sendable {
    func balance() async throws(WalletError) -> Money
    /// Starts a MoMo collection. Returns a ticket to poll; does NOT wait for the USSD push.
    func startTopUp(_ amount: Money, method: TopUpMethod) async throws(WalletError) -> TopUpTicket
    func topUpStatus(_ ticket: TopUpTicket) async throws(WalletError) -> TopUpStatus
}

enum WalletError: Error, Sendable {
    case belowMinimum(Money)      // Campay demo caps a collection at 25 XAF
    case providerRejected(String)
    case network
}
```

```swift
// ios/Packages/ApiClient/Sources/ApiClient/LiveWalletService.swift — the ONLY file that knows "Campay"
actor LiveWalletServicing: WalletServicing {
    private let client: APIClient

    init(client: APIClient) { self.client = client }

    func balance() async throws(WalletError) -> Money {
        do {
            let dto: UserDTO = try await client.get("/user")
            return Money(minorUnits: dto.balanceFcfa, currency: .xaf)   // DTO → domain, here
        } catch {
            throw .network
        }
    }

    func startTopUp(_ amount: Money, method: TopUpMethod) async throws(WalletError) -> TopUpTicket {
        let dto: TopUpDTO = try await client.post("/topup", TopUpRequestDTO(amount: amount, method: method))
        guard let reference = dto.reference else { throw .providerRejected(dto.message ?? "") }
        return TopUpTicket(reference: reference)
    }
}
```

```swift
// ios/Packages/WalletStore/Sources/WalletStoreTestSupport/StubWalletServicing.swift — previews, tests, and offline demo mode
struct StubWalletServicing: WalletServicing {
    var balance: Money = Money(minorUnits: 428_500, currency: .xaf)
    func balance() async throws(WalletError) -> Money { balance }
    func startTopUp(_ a: Money, method: TopUpMethod) async throws(WalletError) -> TopUpTicket { .preview }
    func topUpStatus(_ t: TopUpTicket) async throws(WalletError) -> TopUpStatus { .succeeded }
}

#Preview { HomeView().environment(Store(wallet: StubWalletServicing())) }
```

**The standard**

- All provider and persistence access goes through a `public` protocol declared in `WalletStore`
  (`Sources/WalletStore/WalletServicing.swift`, `CardServicing.swift`); `Money` stays dependency-free.
- Implementations are `actor`s (or `Sendable` structs when stateless) in `ApiClient`; DTO → domain mapping
  happens there and nowhere else.
- **No business logic in a repository.** Fees, FX margins, decline counting, minimum amounts — those are
  domain rules (`Wallet`, `FXRate`), not HTTP concerns. A repository fetches, maps, and returns.
- Repositories are injected (`patterns-dependency-injection.md`); never `Repo.shared`.
- Every protocol ships a stub in `Sources/WalletStoreTestSupport/`, used by `#Preview` and Swift Testing
  (`patterns-dependency-injection.md`).

**Payoff**: replacing Campay with Notch Pay means writing one new actor and changing one line in
`MoniPayApp.swift`. No view, no view model, no test outside `ApiClient` changes.

Reference: [Cal.com Engineering in 2026 and Beyond](https://cal.com/blog/engineering-in-2026-and-beyond)
