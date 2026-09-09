import DesignSystem
import Foundation
import SwiftUI

public struct VirtualCard: Identifiable, Hashable, Sendable {
    public init(
        id: UUID,
        label: String,
        theme: CardTheme,
        network: CardNetwork,
        pan: String,
        cvv: String,
        expiry: String,
        isFrozen: Bool = false,
        createdAt: Date,
        monthlyLimitUSDCents: Int?,
        spentUSDCents: Int,
        singleUse: Bool = false,
        onlineAllowed: Bool = true,
        subscriptionsAllowed: Bool = true,
        declineCount: Int = 0
    ) {
        self.id = id
        self.label = label
        self.theme = theme
        self.network = network
        self.pan = pan
        self.cvv = cvv
        self.expiry = expiry
        self.isFrozen = isFrozen
        self.createdAt = createdAt
        self.monthlyLimitUSDCents = monthlyLimitUSDCents
        self.spentUSDCents = spentUSDCents
        self.singleUse = singleUse
        self.onlineAllowed = onlineAllowed
        self.subscriptionsAllowed = subscriptionsAllowed
        self.declineCount = declineCount
    }

    public let id: UUID
    public var label: String
    public var theme: CardTheme
    public var network: CardNetwork
    public var pan: String
    public var cvv: String
    public var expiry: String
    public var isFrozen = false
    public var createdAt: Date
    public var monthlyLimitUSDCents: Int?
    public var spentUSDCents: Int
    public var singleUse = false
    public var onlineAllowed = true
    public var subscriptionsAllowed = true
    public var declineCount = 0

    public var maskedPan: String { "•••• •••• •••• " + String(pan.suffix(4)) }
    public var last4: String { String(pan.suffix(4)) }

    public var usage: Double {
        guard let limit = monthlyLimitUSDCents, limit > 0 else { return 0 }
        return Double(spentUSDCents) / Double(limit)
    }
}

public enum TxStatus: String, Sendable {
    case approved, pending, declined, refunded

    public var label: LocalizedStringResource {
        switch self {
        case .approved: .money("Approved"); case .pending: .money("Pending")
        case .declined: .money("Declined"); case .refunded: .money("Refunded")
        }
    }
    public var tint: Color {
        switch self {
        case .approved: Brand.credit; case .pending: Brand.pending
        case .declined: Brand.debit; case .refunded: Brand.mark
        }
    }
    public var soft: Color {
        switch self {
        case .approved: Brand.creditSoft; case .pending: Brand.pendingSoft
        case .declined: Brand.debitSoft; case .refunded: Brand.markSoft
        }
    }
    public var symbol: String {
        switch self {
        case .approved: "checkmark"; case .pending: "clock"
        case .declined: "xmark"; case .refunded: "arrow.uturn.backward"
        }
    }
}

public enum TxKind: String, Sendable {
    case payment, topUp, conversion, refund, fee

    public var label: LocalizedStringResource {
        switch self {
        case .payment: .money("Card payment"); case .topUp: .money("Top-up")
        case .conversion: .money("Conversion"); case .refund: .money("Refund")
        case .fee: .money("Fee")
        }
    }
}

public enum TxCategory: String, CaseIterable, Identifiable, Sendable {
    case streaming, software, shopping, transport, food, ads, travel, other
    public var id: String { rawValue }

    public var label: LocalizedStringResource {
        switch self {
        case .streaming: .money("Entertainment"); case .software: .money("Software")
        case .shopping: .money("Shopping"); case .transport: .money("Transport")
        case .food: .money("Dining"); case .ads: .money("Advertising")
        case .travel: .money("Travel"); case .other: .money("Other")
        }
    }
    public var symbol: String {
        switch self {
        case .streaming: "play.tv.fill"; case .software: "chevron.left.forwardslash.chevron.right"
        case .shopping: "bag.fill"; case .transport: "car.fill"
        case .food: "fork.knife"; case .ads: "megaphone.fill"
        case .travel: "airplane"; case .other: "circle.grid.2x2.fill"
        }
    }
    public var slot: Int? {
        switch self {
        case .streaming: 0; case .shopping: 1; case .software: 2; case .food: 3
        case .transport: 4; case .travel: 5; case .ads: 6; case .other: nil
        }
    }
    public var tint: Color { slot.map { Viz.categorical[$0] } ?? Viz.neutral }
}

public struct Transaction: Identifiable, Hashable, Sendable {
    public init(
        id: UUID,
        merchant: String,
        kind: TxKind,
        category: TxCategory,
        status: TxStatus,
        date: Date,
        amountUSDCents: Int,
        amountXAF: Int,
        cardID: UUID?,
        fxRate: Double = 610,
        fxMarginPct: Double = 0.03,
        declineReason: String? = nil
    ) {
        self.id = id
        self.merchant = merchant
        self.kind = kind
        self.category = category
        self.status = status
        self.date = date
        self.amountUSDCents = amountUSDCents
        self.amountXAF = amountXAF
        self.cardID = cardID
        self.fxRate = fxRate
        self.fxMarginPct = fxMarginPct
        self.declineReason = declineReason
    }

    public let id: UUID
    public var merchant: String
    public var kind: TxKind
    public var category: TxCategory
    public var status: TxStatus
    public var date: Date
    public var amountUSDCents: Int
    public var amountXAF: Int
    public var cardID: UUID?
    public var fxRate: Double = 610
    public var fxMarginPct: Double = 0.03
    public var declineReason: String? = nil

    public var isCredit: Bool { amountXAF > 0 }
    public static func == (a: Money.Transaction, b: Money.Transaction) -> Bool { a.id == b.id }
    public func hash(into h: inout Hasher) { h.combine(id) }
}

public struct TopUpMethod: Identifiable, Hashable, Sendable {
    public init(id: String, name: String, detail: String, symbol: String, tint: Color, feePct: Double, instant: Bool) {
        self.id = id
        self.name = name
        self.detail = detail
        self.symbol = symbol
        self.tint = tint
        self.feePct = feePct
        self.instant = instant
    }

    public let id: String
    public var name: String
    public var detail: String
    public var symbol: String
    public var tint: Color
    public var feePct: Double
    public var instant: Bool
}

public struct AppNotification: Identifiable, Sendable {
    public init(title: String, body: String, date: Date, symbol: String, tint: Color, unread: Bool) {
        self.title = title
        self.body = body
        self.date = date
        self.symbol = symbol
        self.tint = tint
        self.unread = unread
    }

    public let id = UUID()
    public var title: String
    public var body: String
    public var date: Date
    public var symbol: String
    public var tint: Color
    public var unread: Bool
}

public struct User {
    public init(firstName: String, lastName: String, phone: String, email: String, kycVerified: Bool) {
        self.firstName = firstName
        self.lastName = lastName
        self.phone = phone
        self.email = email
        self.kycVerified = kycVerified
    }

    public var firstName: String
    public var lastName: String
    public var phone: String
    public var email: String
    public var kycVerified: Bool
    public var initials: String { "\(firstName.prefix(1))\(lastName.prefix(1))" }
    public var fullName: String { "\(firstName) \(lastName)" }
}
