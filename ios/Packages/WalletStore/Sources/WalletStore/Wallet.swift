import Foundation
import Money

public enum AuthDecision: String, Sendable {
    case approved                = "APPROVE"
    case insufficientFunds       = "DECLINE_INSUFFICIENT_FUNDS"
    case cardBlocked             = "DECLINE_CARD_BLOCKED"
    case overSpendLimit          = "DECLINE_SPEND_LIMIT"

    public var isApproved: Bool { self == .approved }
}

public actor Wallet {
    public let ownerId: String
    private(set) var balanceXAF: Int
    private(set) var holds: [String: Int] = [:]
    private(set) var declineCount = 0
    private(set) var isBlocked = false
    private(set) var fx = FXRate()

    public let maxConsecutiveDeclines: Int

    public init(ownerId: String, balanceXAF: Int = 0, maxConsecutiveDeclines: Int = 3) {
        self.ownerId = ownerId
        self.balanceXAF = balanceXAF
        self.maxConsecutiveDeclines = maxConsecutiveDeclines
    }

    public var availableXAF: Int { balanceXAF - holds.values.reduce(0, +) }

    public func topUp(_ xaf: Int) {
        precondition(xaf > 0, "a top-up must be a positive amount")
        balanceXAF += xaf
    }

    public func setFX(_ rate: FXRate) { fx = rate }
    public func unblock() { isBlocked = false; declineCount = 0 }

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

    @discardableResult
    public func capture(authId: String, finalUSDCents: Int? = nil) -> Int? {
        guard let held = holds.removeValue(forKey: authId) else { return nil }
        let debit = finalUSDCents.map { fx.xaf(fromUSDCents: $0) } ?? held
        balanceXAF -= debit
        return debit
    }

    @discardableResult
    public func void(authId: String) -> Int? { holds.removeValue(forKey: authId) }

    private func recordDecline(_ reason: AuthDecision) -> AuthDecision {
        declineCount += 1
        if declineCount >= maxConsecutiveDeclines { isBlocked = true }
        return reason
    }
}
