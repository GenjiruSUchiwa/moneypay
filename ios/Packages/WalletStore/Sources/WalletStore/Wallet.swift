import Foundation
import Money

public enum AuthDecision: String, Sendable {
    case approved                = "APPROVE"
    case insufficientFunds       = "DECLINE_INSUFFICIENT_FUNDS"
    case cardBlocked             = "DECLINE_CARD_BLOCKED"
    case overSpendLimit          = "DECLINE_SPEND_LIMIT"

    public var isApproved: Bool { self == .approved }
}

/// The wallet IS the source of truth for the balance. A virtual card has no
/// account of its own: on every authorization the processor asks us, over a
/// webhook, whether we approve (just-in-time funding). We have about 2 s to
/// answer.
///
/// Model: balance (balanceXAF) plus holds. An authorization freezes funds, a
/// clearing debits them, a void releases them. Available = balance - holds.
public actor Wallet {
    public let ownerId: String
    private(set) var balanceXAF: Int
    private(set) var holds: [String: Int] = [:]   // authId -> FCFA gelés
    private(set) var declineCount = 0
    private(set) var isBlocked = false
    private(set) var fx = FXRate()

    /// Every decline is billed by the processor (about $0.30-0.50 with the
    /// African providers), which is why the card freezes after N consecutive
    /// declines. PaySika does exactly this.
    public let maxConsecutiveDeclines: Int

    public init(ownerId: String, balanceXAF: Int = 0, maxConsecutiveDeclines: Int = 3) {
        self.ownerId = ownerId
        self.balanceXAF = balanceXAF
        self.maxConsecutiveDeclines = maxConsecutiveDeclines
    }

    public var availableXAF: Int { balanceXAF - holds.values.reduce(0, +) }

    public func topUp(_ xaf: Int) {
        precondition(xaf > 0, "recharge <= 0")
        balanceXAF += xaf
    }

    public func setFX(_ rate: FXRate) { fx = rate }
    public func unblock() { isBlocked = false; declineCount = 0 }

    // MARK: - Authorization flow

    /// Just-in-time decision. Idempotent: the same authId presented twice
    /// returns the same answer without freezing again (networks replay
    /// messages).
    public func authorize(authId: String, amountUSDCents: Int, spendLimitUSDCents: Int?) -> AuthDecision {
        if holds[authId] != nil { return .approved }
        if isBlocked { return .cardBlocked }

        if let limit = spendLimitUSDCents, amountUSDCents > limit {
            return recordDecline(.overSpendLimit)
        }

        let needed = fx.xaf(fromUSDCents: amountUSDCents)
        guard availableXAF >= needed else {
            return recordDecline(.insufficientFunds)
        }

        holds[authId] = needed
        declineCount = 0
        return .approved
    }

    /// Clearing: the merchant presents the final amount, which may differ
    /// from the authorization (a tip, a fuel adjustment). We debit the real
    /// one.
    @discardableResult
    public func capture(authId: String, finalUSDCents: Int? = nil) -> Int? {
        guard let held = holds.removeValue(forKey: authId) else { return nil }
        let debit = finalUSDCents.map { fx.xaf(fromUSDCents: $0) } ?? held
        balanceXAF -= debit
        return debit
    }

    /// Void or expiry of the authorization: the held funds are released.
    @discardableResult
    public func void(authId: String) -> Int? { holds.removeValue(forKey: authId) }

    private func recordDecline(_ reason: AuthDecision) -> AuthDecision {
        declineCount += 1
        if declineCount >= maxConsecutiveDeclines { isBlocked = true }
        return reason
    }
}
