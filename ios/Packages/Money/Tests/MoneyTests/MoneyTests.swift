import Testing

@testable import Money

@Suite("Money")
struct MoneyTests {

    @Test("USD to XAF conversion applies the rate and the margin, rounding up")
    func usdCentsConvertToXAF() {
        let rate = FXRate(usdToXAF: 610.0, marginPct: 0.03)
        #expect(rate.xaf(fromUSDCents: 1_000) == 6_283)
        #expect(rate.xaf(fromUSDCents: 1) == 7)
        #expect(rate.xaf(fromUSDCents: 0) == 0)
    }
}
