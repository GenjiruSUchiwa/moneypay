---
title: Protocol-Based Fakes, No Mocking Framework
impact: HIGH
impactDescription: Hand-written fakes compile with the code; mocks drift silently
tags: testing, mocking, fakes, testsupport, dependency-injection, clock
---

## Protocol-Based Fakes, No Mocking Framework

**Impact: HIGH**

MoniPay adds **no mocking library** — no OCMock, no Cuckoo, no Sourcery-generated spies. Every seam
is a small `protocol` with a hand-written `struct` or `actor` fake. A fake that is a real Swift type
fails to compile when the protocol changes; a generated or dynamic mock keeps "passing" against a
signature that no longer exists.

**Where a fake lives decides who can use it:**
- Used by **one** package's own tests → `ios/Packages/<Name>/Tests/<Name>Tests/`.
- Used by **other** packages' tests → a `Sources/<Name>TestSupport/` target, exported as a separate
  library product and depended on only from test targets. `Platform` already ships
  `PlatformTestSupport` with `FixedClock` and `InMemoryKeyValueStoring`; `ApiClient` ships
  `ApiClientTestSupport` with the stub services the feature packages need.
- **Never** in `Sources/<Name>/`. A test double shipped in the app binary is a production liability.

**The three seams that matter:**
1. **Campay collection** (MoMo top-up) — network, slow, and in the demo sandbox capped at 25 XAF.
2. **Sudo Africa card issuing** — network, and creating a real sandbox card on every test run is
   both slow and rate-limited.
3. **Time** — anything that timestamps a `Transaction` or polls for a collection status.

**Rules:**
- The protocol is declared next to the production type it abstracts (`Xxxing.swift`), not in a test.
- Protocols crossing an actor boundary are `Sendable`; fakes are `struct` or `actor`, never a class
  with unprotected mutable state (parallel suites will race). Recording fakes are an `actor` so
  `#expect` can read them safely from another task.
- A fake returns canned data or throws a canned error. It contains **no logic** — if the fake needs
  a branch, the branch belongs in the code under test.
- Never inject a fake by swapping a global or a singleton; pass it through the initialiser.

**Incorrect (untestable: the client is constructed inside, the clock is `Date()`):**

```swift
// ios/Packages/TopUp/Sources/TopUp/TopUpModel.swift
@Observable @MainActor
final class TopUpModel {
    var status: String = ""

    func topUp(xaf: Int) async {
        let client = MoniPayAPIClient(baseURL: URL(string: "http://localhost:8743")!)  // hard-wired
        let result = try? await client.topUp(userID: userID, amountFCFA: xaf)
        // Polling with a real sleep — every test pays 2 real seconds.
        try? await Task.sleep(for: .seconds(2))
        status = result.map { String(localized: "Balance: \($0.fcfa) FCFA") }
            ?? String(localized: "Top-up failed")
    }
}
```

**Correct (protocol seams + injected fakes + injected time):**

```swift
// ios/Packages/ApiClient/Sources/ApiClient/TopUpService.swift — the port ships with the code.
public protocol TopUpService: Sendable {
    func topUp(userID: String, amountFCFA: Int) async throws(MoniPayAPIError) -> TopUpResult
}
public protocol CardIssuing: Sendable {
    func issueCard(userID: String, amountUSD: Decimal) async throws(MoniPayAPIError) -> IssuedCard
}

// ios/Packages/Platform/Sources/Platform/Sleeping.swift
/// Time is a dependency: never a bare `Date()` or `Task.sleep` in production code.
public protocol Sleeping: Sendable { func sleep(for duration: Duration) async throws }
public struct SystemSleeper: Sleeping {
    public init() {}
    public func sleep(for duration: Duration) async throws { try await Task.sleep(for: duration) }
}
```

```swift
// ios/Packages/ApiClient/Sources/ApiClientTestSupport/StubTopUpService.swift
// Exported as .library("ApiClientTestSupport"): reusable by EVERY feature package.
public struct StubTopUpService: TopUpService {
    public var result: Result<TopUpResult, MoniPayAPIError>
    public init(result: Result<TopUpResult, MoniPayAPIError>) { self.result = result }
    public func topUp(userID: String, amountFCFA: Int) async throws(MoniPayAPIError) -> TopUpResult {
        try result.get()
    }
}

/// Recording variant: an actor, because suites run in parallel.
public actor RecordingCardIssuer: CardIssuing {
    public private(set) var requests: [(userID: String, amountUSD: Decimal)] = []
    public var stub: IssuedCard = .sample
    public init() {}
    public func issueCard(userID: String, amountUSD: Decimal)
        async throws(MoniPayAPIError) -> IssuedCard
    { requests.append((userID, amountUSD)); return stub }
}
```

```swift
// ios/Packages/TopUp/Tests/TopUpTests/TopUpModelTests.swift
import ApiClientTestSupport
import PlatformTestSupport   // FixedClock, ImmediateSleeper, InMemoryKeyValueStoring
import Testing
@testable import TopUp

@Suite("TopUpModel")
struct TopUpModelTests {
    @Test("A server-side insufficient balance surfaces as domain copy, not a raw HTTP message")
    @MainActor
    func mapsServerErrorToFrenchMessage() async {
        let model = TopUpModel(
            service: StubTopUpService(
                // Verbatim payload from poc/server.js — wire data, not our copy.
                result: .failure(.rejected(message: "Solde insuffisant: il faut 15700 F"))),
            sleeper: ImmediateSleeper(),   // no real waiting
            clock: FixedClock(now: Date(timeIntervalSince1970: 1_800_000_000))
        )
        await model.topUp(xaf: 25)
        // Assert the state, not the rendered copy: localization is the view's problem.
        #expect(model.status == .failed(.insufficientBalance(neededXAF: 15_700)))
    }
}
```

Wire the support target from the consuming package's **test** target only:

```swift
// ios/Packages/TopUp/Package.swift
.testTarget(
    name: "TopUpTests",
    dependencies: ["TopUp",
                   .product(name: "ApiClientTestSupport", package: "ApiClient"),
                   .product(name: "PlatformTestSupport", package: "Platform")]
)
```

**Never hit Campay or Sudo from a unit test.** The sandbox limits (25 XAF per Campay demo
transaction, shared Sudo funding source) make such a test flaky by construction. The only place real
sandbox calls belong is a manual smoke script such as `poc/smoke-sudo.js`.

Reference: [Swift Testing — Traits and parallel execution](https://developer.apple.com/documentation/testing/parallelization)
