import DesignSystem
import Foundation
import Money
import SwiftUI

@MainActor
@Observable
public final class Store {
    public init() {}

    public var user = User(
        firstName: "Aristide", lastName: "Mbassi",
        phone: "+237 6 99 12 34 56", email: "aristide@moneypay.cm",
        kycVerified: true
    )

    public var balanceXAF: Int = 428_500
    public var fx = FXRate()
    public var cards: [VirtualCard] = SampleData.cards
    public var transactions: [Money.Transaction] = SampleData.transactions
    public var notifications: [AppNotification] = SampleData.notifications
    public var hiddenBalance = false

    public var usdEquivalentCents: Int {
        Int(Double(balanceXAF) / (fx.usdToXAF * (1 + fx.marginPct)) * 100)
    }

    public var unreadCount: Int { notifications.filter(\.unread).count }

    public var activeCards: [VirtualCard] { cards.filter { !$0.isFrozen } }

    public func transactions(for cardID: UUID) -> [Money.Transaction] {
        transactions.filter { $0.cardID == cardID }
    }

    public func grouped(_ txs: [Money.Transaction]) -> [(day: Date, items: [Money.Transaction])] {
        let cal = Calendar.current
        return Dictionary(grouping: txs) { cal.startOfDay(for: $0.date) }
            .map { (day: $0.key, items: $0.value.sorted { $0.date > $1.date }) }
            .sorted { $0.day > $1.day }
    }

    public func spendByCategory(days: Int = 30) -> [(category: TxCategory, xaf: Int)] {
        let since = Date().addingTimeInterval(-Double(days) * 86_400)
        let debits = transactions.filter { $0.date >= since && $0.amountXAF < 0 && $0.status != .declined }
        return Dictionary(grouping: debits, by: \.category)
            .map { (category: $0.key, xaf: -$0.value.reduce(0) { $0 + $1.amountXAF }) }
            .sorted { $0.xaf > $1.xaf }
    }

    public var monthSpendXAF: Int { spendByCategory().reduce(0) { $0 + $1.xaf } }

    public var monthCreditXAF: Int {
        let since = Date().addingTimeInterval(-30 * 86_400)
        return transactions
            .filter { $0.date >= since && $0.amountXAF > 0 && $0.status != .declined }
            .reduce(0) { $0 + $1.amountXAF }
    }

    public func topUp(xaf: Int, method: TopUpMethod) {
        let fee = Int(Double(xaf) * method.feePct)
        balanceXAF += xaf - fee
        transactions.insert(Money.Transaction(
            id: UUID(), merchant: method.name, kind: .topUp, category: .other,
            status: method.instant ? .approved : .pending, date: .now,
            amountUSDCents: 0, amountXAF: xaf - fee, cardID: nil
        ), at: 0)
    }

    @discardableResult
    public func createCard(label: String, theme: CardTheme, network: CardNetwork,
                           limitUSDCents: Int?, singleUse: Bool) -> VirtualCard {
        let card = VirtualCard(
            id: UUID(), label: label.isEmpty ? String(localized: "My card", bundle: .module) : label,
            theme: theme, network: network,
            pan: SampleData.randomPan(), cvv: String(format: "%03d", Int.random(in: 100...999)),
            expiry: SampleData.futureExpiry(), createdAt: .now,
            monthlyLimitUSDCents: limitUSDCents, spentUSDCents: 0, singleUse: singleUse
        )
        cards.insert(card, at: 0)
        return card
    }

    public func toggleFreeze(_ card: VirtualCard) {
        guard let i = cards.firstIndex(where: { $0.id == card.id }) else { return }
        cards[i].isFrozen.toggle()
    }

    public func update(_ card: VirtualCard) {
        guard let i = cards.firstIndex(where: { $0.id == card.id }) else { return }
        cards[i] = card
    }

    public func deleteCard(_ card: VirtualCard) {
        cards.removeAll { $0.id == card.id }
    }

    public func markAllNotificationsRead() {
        for i in notifications.indices { notifications[i].unread = false }
    }
}

public enum SampleData {
    public static var methods: [TopUpMethod] {
        [
            .init(id: "mtn", name: "MTN Mobile Money",
                  detail: String(localized: "•• \("34 56") · instant", bundle: .module),
                  symbol: "antenna.radiowaves.left.and.right", tint: Color(rgb: 0xB88A00),
                  feePct: 0.015, instant: true),
            .init(id: "om", name: "Orange Money",
                  detail: String(localized: "•• \("78 90") · instant", bundle: .module),
                  symbol: "circle.hexagongrid.fill", tint: Color(rgb: 0xC25A0E),
                  feePct: 0.015, instant: true),
            .init(id: "bank", name: String(localized: "Bank transfer", bundle: .module),
                  detail: String(localized: "Afriland First Bank · 1 to 2 days", bundle: .module),
                  symbol: "building.columns.fill", tint: Brand.mark, feePct: 0, instant: false),
            .init(id: "agent", name: String(localized: "MoneyPay agent", bundle: .module),
                  detail: String(localized: "Cash deposit · instant", bundle: .module),
                  symbol: "storefront.fill", tint: Brand.credit, feePct: 0.02, instant: true)
        ]
    }

    public static func randomPan() -> String {
        "5399" + (0..<12).map { _ in String(Int.random(in: 0...9)) }.joined()
    }

    public static func futureExpiry() -> String {
        let d = Calendar.current.date(byAdding: .year, value: 3, to: .now) ?? .now
        let f = DateFormatter(); f.dateFormat = "MM/yy"
        return f.string(from: d)
    }

    public static let cards: [VirtualCard] = [
        VirtualCard(id: UUID(), label: "Abonnements", theme: .ink, network: .mastercard,
                    pan: "5399471028834412", cvv: "417", expiry: "09/29",
                    createdAt: Date().addingTimeInterval(-86_400 * 120),
                    monthlyLimitUSDCents: 15_000, spentUSDCents: 4_780),
        VirtualCard(id: UUID(), label: "Shopping en ligne", theme: .pine, network: .visa,
                    pan: "4539118820047761", cvv: "882", expiry: "03/28",
                    createdAt: Date().addingTimeInterval(-86_400 * 45),
                    monthlyLimitUSDCents: 50_000, spentUSDCents: 31_240),
        VirtualCard(id: UUID(), label: "Serveurs & outils", theme: .clay, network: .mastercard,
                    pan: "5399002914775530", cvv: "205", expiry: "11/27", isFrozen: true,
                    createdAt: Date().addingTimeInterval(-86_400 * 210),
                    monthlyLimitUSDCents: nil, spentUSDCents: 12_900, declineCount: 2)
    ]

    public static var transactions: [Money.Transaction] {
        let c = cards
        func t(_ h: Double) -> Date { Date().addingTimeInterval(-h * 3600) }
        return [
            Money.Transaction(id: UUID(), merchant: "Netflix", kind: .payment, category: .streaming,
                        status: .approved, date: t(3), amountUSDCents: -1_099, amountXAF: -6_907, cardID: c[0].id),
            Money.Transaction(id: UUID(), merchant: "MTN Mobile Money", kind: .topUp, category: .other,
                        status: .approved, date: t(6), amountUSDCents: 0, amountXAF: 98_500, cardID: nil),
            Money.Transaction(id: UUID(), merchant: "OpenAI", kind: .payment, category: .software,
                        status: .approved, date: t(9), amountUSDCents: -2_000, amountXAF: -12_566, cardID: c[1].id),
            Money.Transaction(id: UUID(), merchant: "Amazon", kind: .payment, category: .shopping,
                        status: .declined, date: t(26), amountUSDCents: -8_499, amountXAF: 0, cardID: c[1].id,
                        declineReason: String(localized: "Insufficient balance at authorization",
                                                     bundle: .module)),
            Money.Transaction(id: UUID(), merchant: "Spotify", kind: .payment, category: .streaming,
                        status: .approved, date: t(30), amountUSDCents: -1_199, amountXAF: -7_535, cardID: c[0].id),
            Money.Transaction(id: UUID(), merchant: String(localized: "Decline fee", bundle: .module), kind: .fee, category: .other,
                        status: .approved, date: t(26.1), amountUSDCents: -35, amountXAF: -220, cardID: c[1].id),
            Money.Transaction(id: UUID(), merchant: "Uber", kind: .payment, category: .transport,
                        status: .approved, date: t(52), amountUSDCents: -1_540, amountXAF: -9_678, cardID: c[1].id),
            Money.Transaction(id: UUID(), merchant: "Figma", kind: .payment, category: .software,
                        status: .pending, date: t(55), amountUSDCents: -1_500, amountXAF: -9_427, cardID: c[2].id),
            Money.Transaction(id: UUID(), merchant: "Meta Ads", kind: .payment, category: .ads,
                        status: .approved, date: t(74), amountUSDCents: -5_000, amountXAF: -31_415, cardID: c[1].id),
            Money.Transaction(id: UUID(), merchant: "Orange Money", kind: .topUp, category: .other,
                        status: .approved, date: t(80), amountUSDCents: 0, amountXAF: 150_000, cardID: nil),
            Money.Transaction(id: UUID(), merchant: "Booking.com", kind: .payment, category: .travel,
                        status: .refunded, date: t(98), amountUSDCents: 4_320, amountXAF: 27_143, cardID: c[1].id),
            Money.Transaction(id: UUID(), merchant: "Glovo", kind: .payment, category: .food,
                        status: .approved, date: t(120), amountUSDCents: -890, amountXAF: -5_593, cardID: c[0].id),
            Money.Transaction(id: UUID(), merchant: "DigitalOcean", kind: .payment, category: .software,
                        status: .approved, date: t(146), amountUSDCents: -2_400, amountXAF: -15_080, cardID: c[2].id),
            Money.Transaction(id: UUID(), merchant: "AliExpress", kind: .payment, category: .shopping,
                        status: .approved, date: t(170), amountUSDCents: -3_265, amountXAF: -20_515, cardID: c[1].id)
        ]
    }

    public static var notifications: [AppNotification] {
        [
            .init(title: String(localized: "Payment approved", bundle: .module),
                  body: String(localized: "Netflix · \(Fmt.usd(1_099)) charged to “\(cards[0].label)”",
                               bundle: .module),
                  date: Date().addingTimeInterval(-3 * 3600), symbol: "checkmark.circle.fill",
                  tint: Brand.credit, unread: true),
            .init(title: String(localized: "Top-up received", bundle: .module),
                  body: String(localized: "\(Fmt.xaf(98_500)) from MTN Mobile Money", bundle: .module),
                  date: Date().addingTimeInterval(-6 * 3600), symbol: "arrow.down.circle.fill",
                  tint: Brand.credit, unread: true),
            .init(title: String(localized: "Payment declined", bundle: .module),
                  body: String(localized: "Amazon · insufficient balance. \(2) declines left before a block.",
                               bundle: .module),
                  date: Date().addingTimeInterval(-26 * 3600), symbol: "xmark.circle.fill",
                  tint: Brand.debit, unread: true),
            .init(title: String(localized: "Card frozen", bundle: .module),
                  body: String(localized: "“\(cards[2].label)” was frozen from the app", bundle: .module),
                  date: Date().addingTimeInterval(-40 * 3600), symbol: "snowflake",
                  tint: Brand.mark, unread: false),
            .init(title: String(localized: "Today's rate", bundle: .module),
                  body: String(localized: "1 USD = \(Fmt.xaf(610)) · MoneyPay margin \(0.03, format: .percent)",
                               bundle: .module),
                  date: Date().addingTimeInterval(-70 * 3600), symbol: "arrow.left.arrow.right",
                  tint: Brand.inkMuted, unread: false)
        ]
    }
}
