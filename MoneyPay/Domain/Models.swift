import SwiftUI

// MARK: - Carte

struct VirtualCard: Identifiable, Hashable {
    let id: UUID
    var label: String
    var theme: CardTheme
    var network: CardNetwork
    var pan: String
    var cvv: String
    var expiry: String
    var isFrozen = false
    var createdAt: Date
    /// Plafond mensuel en cents USD. nil = pas de plafond.
    var monthlyLimitUSDCents: Int?
    var spentUSDCents: Int
    var singleUse = false
    var onlineAllowed = true
    var subscriptionsAllowed = true
    var declineCount = 0

    var maskedPan: String { "•••• •••• •••• " + String(pan.suffix(4)) }
    var last4: String { String(pan.suffix(4)) }

    var usage: Double {
        guard let limit = monthlyLimitUSDCents, limit > 0 else { return 0 }
        return Double(spentUSDCents) / Double(limit)
    }
}

// MARK: - Transaction

enum TxStatus: String {
    case approved, pending, declined, refunded

    var label: String {
        switch self {
        case .approved: "Réussi"; case .pending: "En attente"
        case .declined: "Refusé"; case .refunded: "Remboursé"
        }
    }
    var tint: Color {
        switch self {
        case .approved: Brand.credit; case .pending: Brand.pending
        case .declined: Brand.debit; case .refunded: Brand.mark
        }
    }
    var soft: Color {
        switch self {
        case .approved: Brand.creditSoft; case .pending: Brand.pendingSoft
        case .declined: Brand.debitSoft; case .refunded: Brand.markSoft
        }
    }
    var symbol: String {
        switch self {
        case .approved: "checkmark"; case .pending: "clock"
        case .declined: "xmark"; case .refunded: "arrow.uturn.backward"
        }
    }
}

enum TxKind: String {
    case payment, topUp, conversion, refund, fee

    var label: String {
        switch self {
        case .payment: "Paiement carte"; case .topUp: "Rechargement"
        case .conversion: "Conversion"; case .refund: "Remboursement"; case .fee: "Frais"
        }
    }
}

enum TxCategory: String, CaseIterable, Identifiable {
    case streaming, software, shopping, transport, food, ads, travel, other
    var id: String { rawValue }

    var label: String {
        switch self {
        case .streaming: "Divertissement"; case .software: "Logiciels"
        case .shopping: "Achats"; case .transport: "Transport"
        case .food: "Restauration"; case .ads: "Publicité"
        case .travel: "Voyage"; case .other: "Autre"
        }
    }
    var symbol: String {
        switch self {
        case .streaming: "play.tv.fill"; case .software: "chevron.left.forwardslash.chevron.right"
        case .shopping: "bag.fill"; case .transport: "car.fill"
        case .food: "fork.knife"; case .ads: "megaphone.fill"
        case .travel: "airplane"; case .other: "circle.grid.2x2.fill"
        }
    }
    /// Slot catégoriel fixe. L'ordre ne change jamais : c'est lui qui garantit
    /// la séparation daltonisme. « Autre » prend le neutre réservé.
    var slot: Int? {
        switch self {
        case .streaming: 0; case .shopping: 1; case .software: 2; case .food: 3
        case .transport: 4; case .travel: 5; case .ads: 6; case .other: nil
        }
    }
    var tint: Color { slot.map { Viz.categorical[$0] } ?? Viz.neutral }
}

struct Transaction: Identifiable, Hashable {
    let id: UUID
    var merchant: String
    var kind: TxKind
    var category: TxCategory
    var status: TxStatus
    var date: Date
    /// Montant présenté par le marchand, en cents USD (négatif = débit).
    var amountUSDCents: Int
    /// Montant réellement mouvementé sur le wallet, en FCFA.
    var amountXAF: Int
    var cardID: UUID?
    var fxRate: Double = 610
    var fxMarginPct: Double = 0.03
    var declineReason: String? = nil

    var isCredit: Bool { amountXAF > 0 }
    static func == (a: Transaction, b: Transaction) -> Bool { a.id == b.id }
    func hash(into h: inout Hasher) { h.combine(id) }
}

// MARK: - Rechargement

struct TopUpMethod: Identifiable, Hashable {
    let id: String
    var name: String
    var detail: String
    var symbol: String
    var tint: Color
    var feePct: Double
    var instant: Bool
}

// MARK: - Notification

struct AppNotification: Identifiable {
    let id = UUID()
    var title: String
    var body: String
    var date: Date
    var symbol: String
    var tint: Color
    var unread: Bool
}

// MARK: - Utilisateur

struct User {
    var firstName: String
    var lastName: String
    var phone: String
    var email: String
    var kycVerified: Bool
    var initials: String { "\(firstName.prefix(1))\(lastName.prefix(1))" }
    var fullName: String { "\(firstName) \(lastName)" }
}
