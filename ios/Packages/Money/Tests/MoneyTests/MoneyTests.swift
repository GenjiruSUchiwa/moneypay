import Testing

@testable import Money

/// Smoke tests for `Money.swift`: amounts are integer minor units (XAF has no
/// decimals, USD is cents) and conversion rounds up to the next XAF, FX margin
/// included.
@Suite("Money")
struct MoneyTests {

    @Test("USD to XAF conversion applies the rate and the margin, rounding up")
    func usdCentsConvertToXAF() {
        let rate = FXRate(usdToXAF: 610.0, marginPct: 0.03)
        // $10.00 -> 10 * 610 * 1.03 = exactly 6283 XAF.
        #expect(rate.xaf(fromUSDCents: 1_000) == 6_283)
        // $0.01 -> 6.283 XAF, rounded up: the fintech never loses on
        // rounding.
        #expect(rate.xaf(fromUSDCents: 1) == 7)
        #expect(rate.xaf(fromUSDCents: 0) == 0)
    }
}
