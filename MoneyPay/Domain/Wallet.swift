import Foundation

enum AuthDecision: String, Sendable {
    case approved                = "APPROVE"
    case insufficientFunds       = "DECLINE_INSUFFICIENT_FUNDS"
    case cardBlocked             = "DECLINE_CARD_BLOCKED"
    case overSpendLimit          = "DECLINE_SPEND_LIMIT"

    var isApproved: Bool { self == .approved }
}

/// Le wallet EST la source de vérité du solde. La carte virtuelle n'a pas de
/// compte propre : à chaque autorisation le processeur nous demande, via webhook,
/// si on approuve (just-in-time funding). On a ~2 s pour répondre.
///
/// Modèle : solde (balanceXAF) + gels (holds). Une autorisation gèle, un
/// clearing débite, un void libère. Le disponible = solde - gels.
actor Wallet {
    let ownerId: String
    private(set) var balanceXAF: Int
    private(set) var holds: [String: Int] = [:]   // authId -> FCFA gelés
    private(set) var declineCount = 0
    private(set) var isBlocked = false
    private(set) var fx = FXRate()

    /// Chaque déclin est facturé par le processeur (~0,3-0,5 $ chez les providers
    /// africains). D'où le gel de la carte après N déclins consécutifs — c'est
    /// exactement ce que fait PaySika.
    let maxConsecutiveDeclines: Int

    init(ownerId: String, balanceXAF: Int = 0, maxConsecutiveDeclines: Int = 3) {
        self.ownerId = ownerId
        self.balanceXAF = balanceXAF
        self.maxConsecutiveDeclines = maxConsecutiveDeclines
    }

    var availableXAF: Int { balanceXAF - holds.values.reduce(0, +) }

    func topUp(_ xaf: Int) {
        precondition(xaf > 0, "recharge <= 0")
        balanceXAF += xaf
    }

    func setFX(_ rate: FXRate) { fx = rate }
    func unblock() { isBlocked = false; declineCount = 0 }

    // MARK: - Flux d'autorisation

    /// Décision just-in-time. Idempotent : un même authId re-présenté renvoie la
    /// même réponse sans re-geler (le réseau rejoue les messages).
    func authorize(authId: String, amountUSDCents: Int, spendLimitUSDCents: Int?) -> AuthDecision {
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

    /// Clearing : le marchand présente le montant final (peut différer de
    /// l'autorisation — pourboire, ajustement essence). On débite le réel.
    @discardableResult
    func capture(authId: String, finalUSDCents: Int? = nil) -> Int? {
        guard let held = holds.removeValue(forKey: authId) else { return nil }
        let debit = finalUSDCents.map { fx.xaf(fromUSDCents: $0) } ?? held
        balanceXAF -= debit
        return debit
    }

    /// Void / expiration de l'autorisation : on relâche les fonds gelés.
    @discardableResult
    func void(authId: String) -> Int? { holds.removeValue(forKey: authId) }

    private func recordDecline(_ reason: AuthDecision) -> AuthDecision {
        declineCount += 1
        if declineCount >= maxConsecutiveDeclines { isBlocked = true }
        return reason
    }
}
