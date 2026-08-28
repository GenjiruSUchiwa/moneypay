import Foundation

/// Everything in integer minor units. Never a Double for a balance.
/// USD becomes cents. XAF has no decimals: one unit is one XAF.
/// `nonisolated`: a Sendable value type of pure arithmetic. It has to stay
/// callable from `actor Wallet`, which decides authorizations off the main
/// actor, so it must not inherit the module's @MainActor default isolation.
nonisolated public struct FXRate: Sendable {
    public init(usdToXAF: Double = 610.0, marginPct: Double = 0.03) {
        self.usdToXAF = usdToXAF
        self.marginPct = marginPct
    }

    /// ponytail: rate hard-coded. In production, pull it from a feed and
    /// refresh it (BEAC for the fixed EUR/XAF peg, an FX provider for USD/EUR).
    public var usdToXAF: Double = 610.0

    /// FX margin the fintech applies. This, not card fees, is where the real
    /// business margin sits (2-4%). It is the calibration knob.
    public var marginPct: Double = 0.03

    /// Converts a card amount (USD cents) into the XAF to debit from the
    /// wallet. Always rounds up to the next XAF: the fintech never loses on
    /// rounding.
    public func xaf(fromUSDCents cents: Int) -> Int {
        precondition(cents >= 0, "montant négatif")
        return Int((Double(cents) / 100.0 * usdToXAF * (1.0 + marginPct)).rounded(.up))
    }
}

/// `nonisolated` for the same reason as `FXRate`: formatting a minor-unit
/// amount is pure and is needed on whichever actor holds the value.
nonisolated public extension Int {
    var xafLabel: String { "\(self) FCFA" }
    var usdLabel: String { String(format: "$%.2f", Double(self) / 100.0) }
}
