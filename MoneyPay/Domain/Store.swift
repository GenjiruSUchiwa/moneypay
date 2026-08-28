import SwiftUI

/// Source de vérité unique de l'UI. Données de démonstration en mémoire :
/// l'écran est branché sur ce store, on remplacera son intérieur par les appels
/// à l'issuer (Lithic) sans toucher aux vues.
@Observable
final class Store {
    var user = User(
        firstName: "Aristide", lastName: "Mbassi",
        phone: "+237 6 99 12 34 56", email: "aristide@moneypay.cm",
        kycVerified: true
    )

    var balanceXAF: Int = 428_500
    var fx = FXRate()
    var cards: [VirtualCard] = SampleData.cards
    var transactions: [Transaction] = SampleData.transactions
    var notifications: [AppNotification] = SampleData.notifications
    var hiddenBalance = false

    // MARK: Dérivés

    var usdEquivalentCents: Int {
        Int(Double(balanceXAF) / (fx.usdToXAF * (1 + fx.marginPct)) * 100)
    }

    var unreadCount: Int { notifications.filter(\.unread).count }

    var activeCards: [VirtualCard] { cards.filter { !$0.isFrozen } }

    func transactions(for cardID: UUID) -> [Transaction] {
        transactions.filter { $0.cardID == cardID }
    }

    /// Regroupement par jour, du plus récent au plus ancien — la structure de
    /// liste utilisée par Monzo, Starling et N26.
    func grouped(_ txs: [Transaction]) -> [(day: Date, items: [Transaction])] {
        let cal = Calendar.current
        return Dictionary(grouping: txs) { cal.startOfDay(for: $0.date) }
            .map { (day: $0.key, items: $0.value.sorted { $0.date > $1.date }) }
            .sorted { $0.day > $1.day }
    }

    func spendByCategory(days: Int = 30) -> [(category: TxCategory, xaf: Int)] {
        let since = Date().addingTimeInterval(-Double(days) * 86_400)
        let debits = transactions.filter { $0.date >= since && $0.amountXAF < 0 && $0.status != .declined }
        return Dictionary(grouping: debits, by: \.category)
            .map { (category: $0.key, xaf: -$0.value.reduce(0) { $0 + $1.amountXAF }) }
            .sorted { $0.xaf > $1.xaf }
    }

    var monthSpendXAF: Int { spendByCategory().reduce(0) { $0 + $1.xaf } }

    var monthCreditXAF: Int {
        let since = Date().addingTimeInterval(-30 * 86_400)
        return transactions
            .filter { $0.date >= since && $0.amountXAF > 0 && $0.status != .declined }
            .reduce(0) { $0 + $1.amountXAF }
    }

    // MARK: Actions

    func topUp(xaf: Int, method: TopUpMethod) {
        let fee = Int(Double(xaf) * method.feePct)
        balanceXAF += xaf - fee
        transactions.insert(Transaction(
            id: UUID(), merchant: method.name, kind: .topUp, category: .other,
            status: method.instant ? .approved : .pending, date: .now,
            amountUSDCents: 0, amountXAF: xaf - fee, cardID: nil
        ), at: 0)
    }

    @discardableResult
    func createCard(label: String, theme: CardTheme, network: CardNetwork,
                    limitUSDCents: Int?, singleUse: Bool) -> VirtualCard {
        let card = VirtualCard(
            id: UUID(), label: label.isEmpty ? "Ma carte" : label,
            theme: theme, network: network,
            pan: SampleData.randomPan(), cvv: String(format: "%03d", Int.random(in: 100...999)),
            expiry: SampleData.futureExpiry(), createdAt: .now,
            monthlyLimitUSDCents: limitUSDCents, spentUSDCents: 0, singleUse: singleUse
        )
        cards.insert(card, at: 0)
        return card
    }

    func toggleFreeze(_ card: VirtualCard) {
        guard let i = cards.firstIndex(where: { $0.id == card.id }) else { return }
        cards[i].isFrozen.toggle()
        Haptic.success()
    }

    func update(_ card: VirtualCard) {
        guard let i = cards.firstIndex(where: { $0.id == card.id }) else { return }
        cards[i] = card
    }

    func deleteCard(_ card: VirtualCard) {
        cards.removeAll { $0.id == card.id }
    }

    func markAllNotificationsRead() {
        for i in notifications.indices { notifications[i].unread = false }
    }
}

// MARK: - Données de démonstration

enum SampleData {
    static let methods: [TopUpMethod] = [
        .init(id: "mtn", name: "MTN Mobile Money", detail: "•• 34 56 · instantané",
              symbol: "antenna.radiowaves.left.and.right", tint: Color(rgb: 0xB88A00), feePct: 0.015, instant: true),
        .init(id: "om", name: "Orange Money", detail: "•• 78 90 · instantané",
              symbol: "circle.hexagongrid.fill", tint: Color(rgb: 0xC25A0E), feePct: 0.015, instant: true),
        .init(id: "bank", name: "Virement bancaire", detail: "Afriland First Bank · 1 à 2 jours",
              symbol: "building.columns.fill", tint: Brand.mark, feePct: 0, instant: false),
        .init(id: "agent", name: "Agent MoneyPay", detail: "Dépôt en espèces · instantané",
              symbol: "storefront.fill", tint: Brand.credit, feePct: 0.02, instant: true)
    ]

    static func randomPan() -> String {
        "5399" + (0..<12).map { _ in String(Int.random(in: 0...9)) }.joined()
    }

    static func futureExpiry() -> String {
        let d = Calendar.current.date(byAdding: .year, value: 3, to: .now) ?? .now
        let f = DateFormatter(); f.dateFormat = "MM/yy"
        return f.string(from: d)
    }

    static let cards: [VirtualCard] = [
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

    static let transactions: [Transaction] = {
        let c = cards
        func t(_ h: Double) -> Date { Date().addingTimeInterval(-h * 3600) }
        return [
            Transaction(id: UUID(), merchant: "Netflix", kind: .payment, category: .streaming,
                        status: .approved, date: t(3), amountUSDCents: -1_099, amountXAF: -6_907, cardID: c[0].id),
            Transaction(id: UUID(), merchant: "MTN Mobile Money", kind: .topUp, category: .other,
                        status: .approved, date: t(6), amountUSDCents: 0, amountXAF: 98_500, cardID: nil),
            Transaction(id: UUID(), merchant: "OpenAI", kind: .payment, category: .software,
                        status: .approved, date: t(9), amountUSDCents: -2_000, amountXAF: -12_566, cardID: c[1].id),
            Transaction(id: UUID(), merchant: "Amazon", kind: .payment, category: .shopping,
                        status: .declined, date: t(26), amountUSDCents: -8_499, amountXAF: 0, cardID: c[1].id,
                        declineReason: "Solde insuffisant au moment de l'autorisation"),
            Transaction(id: UUID(), merchant: "Spotify", kind: .payment, category: .streaming,
                        status: .approved, date: t(30), amountUSDCents: -1_199, amountXAF: -7_535, cardID: c[0].id),
            Transaction(id: UUID(), merchant: "Frais de refus", kind: .fee, category: .other,
                        status: .approved, date: t(26.1), amountUSDCents: -35, amountXAF: -220, cardID: c[1].id),
            Transaction(id: UUID(), merchant: "Uber", kind: .payment, category: .transport,
                        status: .approved, date: t(52), amountUSDCents: -1_540, amountXAF: -9_678, cardID: c[1].id),
            Transaction(id: UUID(), merchant: "Figma", kind: .payment, category: .software,
                        status: .pending, date: t(55), amountUSDCents: -1_500, amountXAF: -9_427, cardID: c[2].id),
            Transaction(id: UUID(), merchant: "Meta Ads", kind: .payment, category: .ads,
                        status: .approved, date: t(74), amountUSDCents: -5_000, amountXAF: -31_415, cardID: c[1].id),
            Transaction(id: UUID(), merchant: "Orange Money", kind: .topUp, category: .other,
                        status: .approved, date: t(80), amountUSDCents: 0, amountXAF: 150_000, cardID: nil),
            Transaction(id: UUID(), merchant: "Booking.com", kind: .payment, category: .travel,
                        status: .refunded, date: t(98), amountUSDCents: 4_320, amountXAF: 27_143, cardID: c[1].id),
            Transaction(id: UUID(), merchant: "Glovo", kind: .payment, category: .food,
                        status: .approved, date: t(120), amountUSDCents: -890, amountXAF: -5_593, cardID: c[0].id),
            Transaction(id: UUID(), merchant: "DigitalOcean", kind: .payment, category: .software,
                        status: .approved, date: t(146), amountUSDCents: -2_400, amountXAF: -15_080, cardID: c[2].id),
            Transaction(id: UUID(), merchant: "AliExpress", kind: .payment, category: .shopping,
                        status: .approved, date: t(170), amountUSDCents: -3_265, amountXAF: -20_515, cardID: c[1].id)
        ]
    }()

    static let notifications: [AppNotification] = [
        .init(title: "Paiement autorisé", body: "Netflix · $10,99 débité de « Abonnements »",
              date: Date().addingTimeInterval(-3 * 3600), symbol: "checkmark.circle.fill",
              tint: Brand.credit, unread: true),
        .init(title: "Rechargement reçu", body: "98 500 FCFA depuis MTN Mobile Money",
              date: Date().addingTimeInterval(-6 * 3600), symbol: "arrow.down.circle.fill",
              tint: Brand.credit, unread: true),
        .init(title: "Paiement refusé", body: "Amazon · solde insuffisant. 2 refus restants avant blocage.",
              date: Date().addingTimeInterval(-26 * 3600), symbol: "xmark.circle.fill",
              tint: Brand.debit, unread: true),
        .init(title: "Carte gelée", body: "« Serveurs & outils » a été gelée depuis l'application",
              date: Date().addingTimeInterval(-40 * 3600), symbol: "snowflake",
              tint: Brand.mark, unread: false),
        .init(title: "Taux du jour", body: "1 USD = 610 FCFA · marge MoneyPay 3 %",
              date: Date().addingTimeInterval(-70 * 3600), symbol: "arrow.left.arrow.right",
              tint: Brand.inkMuted, unread: false)
    ]
}
