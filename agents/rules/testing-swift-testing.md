---
title: Use Swift Testing for Unit Tests, Not XCTest
impact: HIGH
impactDescription: Parallel-by-default, value-semantics suites catch state leakage XCTest hides
tags: testing, swift-testing, xctest, concurrency
---

## Use Swift Testing for Unit Tests, Not XCTest

**Impact: HIGH**

All unit tests use the Swift Testing framework (`import Testing`): `@Test`, `#expect`, `#require`,
`@Suite`. XCTest stays only where Swift Testing has no equivalent — UI automation (`XCUIApplication`)
and performance measurement. Never mix `XCTAssert*` into a `@Test` function; the failure will not be
attributed correctly.

Why it matters here: Swift Testing suites are **structs instantiated fresh per test** and run **in
parallel by default**. That surfaces exactly the bugs a money app cannot ship — a shared `Store`
mutated across tests, a `Wallet` actor reused between cases, a global `FXRate` someone forgot to
reset. `#expect` also captures the sub-expressions of a failing expression, so `#expect(decision ==
.approved)` tells you what `decision` actually was without a message string.

**Rules:**
- One `@Suite` per unit under test; name it after the type, not the file.
- Test names are sentences describing the behaviour, in `@Test("...")`, not in the function name.
- `#expect` for assertions that should continue; `#require` when the rest of the test is meaningless
  without the value (it throws and stops the test).
- Table-driven cases go through `@Test(arguments:)`, never a `for` loop inside one test — one
  argument row failing must not hide the others.
- **Tests live inside the package that owns the code**: `ios/Packages/<Name>/Tests/<Name>Tests/`,
  mirroring `Sources/<Name>/`. `Sources/WalletStore/Wallet.swift` →
  `Tests/WalletStoreTests/WalletTests.swift`. Only composition-root tests go in `ios/Tests/`.
- Run them with `swift test --package-path ios/Packages/<Name>` — no simulator, no Xcode project.

**Incorrect (XCTest habits: shared state, loop over cases, opaque failure):**

```swift
// ios/Packages/WalletStore/Tests/WalletStoreTests/WalletTests.swift
import XCTest

final class WalletTests: XCTestCase {
    // Shared across every test method — one test's holds leak into the next.
    static let wallet = Wallet(ownerId: "u1", balanceXAF: 100_000)

    func testAuthorize() async {
        for cents in [1_000, 5_000, 50_000] {
            let d = await Self.wallet.authorize(authId: "a", amountUSDCents: cents,
                                                spendLimitUSDCents: nil)
            XCTAssertTrue(d.isApproved)   // which `cents` failed? The message doesn't say.
        }
    }
}
```

**Correct (fresh instance per test, parameterized, self-describing failures):**

```swift
// ios/Packages/WalletStore/Tests/WalletStoreTests/WalletTests.swift
import Money
import Testing
@testable import WalletStore

@Suite("Wallet — just-in-time authorization")
struct WalletTests {
    // A stored property is re-initialised for every @Test: no leakage between cases.
    let wallet = Wallet(ownerId: "u1", balanceXAF: 100_000, maxConsecutiveDeclines: 3)

    @Test("Approves while the available balance covers the converted amount",
          arguments: [1_00, 50_00, 100_00])
    func approvesWithinBalance(cents: Int) async {
        let decision = await wallet.authorize(authId: "auth-\(cents)",
                                              amountUSDCents: cents,
                                              spendLimitUSDCents: nil)
        #expect(decision == .approved)
        #expect(await wallet.availableXAF < 100_000)
    }

    @Test("A replayed authId does not place a second hold (the network replays messages)")
    func replayIsIdempotent() async {
        _ = await wallet.authorize(authId: "auth-1", amountUSDCents: 10_00,
                                   spendLimitUSDCents: nil)
        let available = await wallet.availableXAF
        let replay = await wallet.authorize(authId: "auth-1", amountUSDCents: 10_00,
                                            spendLimitUSDCents: nil)
        #expect(replay == .approved)
        #expect(await wallet.availableXAF == available)
    }

    @Test("Capture debits the final amount, not the held amount")
    func captureUsesFinalAmount() async throws {
        _ = await wallet.authorize(authId: "auth-1", amountUSDCents: 10_00,
                                   spendLimitUSDCents: nil)
        // #require: if capture returns nil, the rest of the test is meaningless.
        let debited = try #require(await wallet.capture(authId: "auth-1", finalUSDCents: 12_00))
        #expect(debited == FXRate().xaf(fromUSDCents: 12_00))
    }
}
```

**Traits worth knowing:** `.tags(.money)` to group slow or critical suites (declare with
`extension Tag { @Tag static var money: Self }`), `.serialized` on a suite that genuinely cannot run
in parallel, `.disabled("raison")` and `.bug("<url>")` instead of commenting a test out,
`.timeLimit(.minutes(1))` on anything that awaits a network fake. Use `withKnownIssue { }` for a
failure you have accepted and tracked — never a silently deleted assertion.

**`@testable import` stays inside the package.** A test in `MoneyTests` may reach into `Money`'s
internals; it must never `@testable import` a *different* package — that is a design smell saying
the API you need is not `public` yet, or the test is in the wrong package.

Reference: [Swift Testing documentation](https://developer.apple.com/documentation/testing)
