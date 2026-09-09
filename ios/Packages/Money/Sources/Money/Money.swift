import Foundation

nonisolated public struct FXRate: Sendable {
    public init(usdToXAF: Double = 610.0, marginPct: Double = 0.03) {
        self.usdToXAF = usdToXAF
        self.marginPct = marginPct
    }

    public var usdToXAF: Double = 610.0

    public var marginPct: Double = 0.03

    public func xaf(fromUSDCents cents: Int) -> Int {
        precondition(cents >= 0, "the amount must not be negative")
        return Int((Double(cents) / 100.0 * usdToXAF * (1.0 + marginPct)).rounded(.up))
    }
}
