import SwiftUI

// MARK: - Ligne

struct TransactionRow: View {
    var tx: Transaction

    var body: some View {
        HStack(spacing: 12) {
            IconTile(symbol: tx.kind == .topUp ? "arrow.down" : tx.category.symbol, tint: tint)

            VStack(alignment: .leading, spacing: 2) {
                HStack(spacing: 6) {
                    Text(tx.merchant).font(.body).foregroundStyle(Brand.ink).lineLimit(1)
                    if tx.status != .approved {
                        StatusPill(text: tx.status.label, symbol: tx.status.symbol,
                                   tint: tx.status.tint, soft: tx.status.soft)
                    }
                }
                Text("\(tx.kind.label) · \(Fmt.time(tx.date))")
                    .font(.sub).foregroundStyle(Brand.inkMuted).lineLimit(1)
            }
            Spacer(minLength: 10)

            VStack(alignment: .trailing, spacing: 2) {
                if tx.status == .declined {
                    // Rien n'a été débité : on le dit, plutôt que de barrer un zéro.
                    Text("—").font(.bodyMed).foregroundStyle(Brand.inkFaint)
                } else {
                    MoneyText.xaf(tx.amountXAF, size: 16, weight: .medium,
                                  color: tx.amountXAF > 0 ? Brand.credit : Brand.ink,
                                  signed: true, unit: false)
                }
                if tx.amountUSDCents != 0 {
                    Text(Fmt.usd(tx.amountUSDCents)).font(.micro).foregroundStyle(Brand.inkFaint)
                }
            }
        }
        .padding(.vertical, 13)
        .contentShape(.rect)
    }

    private var tint: Color {
        switch tx.kind {
        case .topUp: Brand.credit
        case .fee: Brand.pending
        default: tx.category.tint
        }
    }
}

// MARK: - Liste

struct TransactionsView: View {
    @Environment(Store.self) private var store
    @State private var filter = 0
    @State private var query = ""

    private let filters = ["Tout", "Cartes", "Recharges", "Refusés"]

    private var filtered: [Transaction] {
        var out = store.transactions
        switch filter {
        case 1: out = out.filter { $0.kind == .payment }
        case 2: out = out.filter { $0.kind == .topUp }
        case 3: out = out.filter { $0.status == .declined }
        default: break
        }
        if !query.isEmpty { out = out.filter { $0.merchant.localizedCaseInsensitiveContains(query) } }
        return out
    }

    var body: some View {
        NavigationStack {
            ScrollView {
                LazyVStack(alignment: .leading, spacing: 0) {
                    ForEach(store.grouped(filtered), id: \.day) { group in
                        HStack {
                            Eyebrow(text: Fmt.relativeDay(group.day))
                            Spacer()
                            Text(dayTotal(group.items)).font(.micro).foregroundStyle(Brand.inkFaint)
                                .monospacedDigit()
                        }
                        .gutter()
                        .padding(.top, 22)
                        .padding(.bottom, 2)

                        ForEach(group.items) { tx in
                            NavigationLink { TransactionDetailView(tx: tx) } label: {
                                TransactionRow(tx: tx)
                            }
                            .buttonStyle(.plain)
                            .gutter()
                        }
                    }

                    if filtered.isEmpty {
                        EmptyNote(title: "Aucun résultat",
                                  message: "Essayez un autre filtre ou un autre nom de marchand.")
                            .gutter()
                    }
                }
                .padding(.bottom, 26)
            }
            .scrollIndicators(.hidden)
            .page()
            .safeAreaInset(edge: .top, spacing: 0) {
                VStack(alignment: .leading, spacing: 14) {
                    HStack {
                        Text("Activité").font(.title1).tight(-0.6).foregroundStyle(Brand.ink)
                        Spacer()
                        Menu {
                            Button("Exporter en PDF", systemImage: "arrow.down.doc") {}
                            Button("Exporter en CSV", systemImage: "tablecells") {}
                        } label: {
                            Image(systemName: "square.and.arrow.up")
                                .font(.system(size: 16)).foregroundStyle(Brand.ink)
                                .frame(width: 34, height: 34)
                        }
                    }
                    Segments(items: filters, selection: $filter)
                }
                .gutter()
                .padding(.top, 4)
                .background(Brand.bg)
                .overlay(alignment: .bottom) { Rule() }
            }
            .searchable(text: $query, prompt: "Rechercher un marchand")
        }
    }

    private func dayTotal(_ items: [Transaction]) -> String {
        let net = items.filter { $0.status != .declined }.reduce(0) { $0 + $1.amountXAF }
        return (net > 0 ? "+" : net < 0 ? "−" : "") + Fmt.group(net) + " FCFA"
    }
}

// MARK: - Détail

struct TransactionDetailView: View {
    var tx: Transaction
    @Environment(Store.self) private var store
    @State private var toastMsg: Toast?

    private var card: VirtualCard? { store.cards.first { $0.id == tx.cardID } }
    private var gross: Int { Int(Double(abs(tx.amountUSDCents)) / 100 * tx.fxRate) }
    private var margin: Int { max(0, abs(tx.amountXAF) - gross) }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                header
                if tx.status == .declined { declineNote }
                Rule().padding(.top, Metric.section)
                breakdown
                Rule()
                if let card { paidWith(card) ; Rule() }
                actions
            }
            .padding(.bottom, 30)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("")
        .navigationBarTitleDisplayMode(.inline)
        .toast($toastMsg)
    }

    private var header: some View {
        VStack(alignment: .leading, spacing: 0) {
            IconTile(symbol: tx.kind == .topUp ? "arrow.down" : tx.category.symbol,
                     tint: tx.kind == .topUp ? Brand.credit : tx.category.tint, size: 44)

            Text(tx.merchant).font(.title3).foregroundStyle(Brand.inkMuted).padding(.top, 16)

            Group {
                if tx.amountUSDCents != 0 {
                    MoneyText.usd(tx.amountUSDCents, size: 36,
                                  color: tx.status == .declined ? Brand.inkFaint : Brand.ink)
                } else {
                    MoneyText.xaf(tx.amountXAF, size: 36,
                                  color: tx.amountXAF > 0 ? Brand.credit : Brand.ink, signed: true)
                }
            }
            .padding(.top, 4)

            HStack(spacing: 8) {
                StatusPill(text: tx.status.label, symbol: tx.status.symbol,
                           tint: tx.status.tint, soft: tx.status.soft)
                Text(Fmt.fullDate(tx.date)).font(.sub).foregroundStyle(Brand.inkMuted)
            }
            .padding(.top, 12)
        }
        .gutter()
        .padding(.top, 4)
    }

    private var declineNote: some View {
        HStack(alignment: .top, spacing: 10) {
            Image(systemName: "exclamationmark.triangle.fill")
                .font(.system(size: 13)).foregroundStyle(Brand.debit).padding(.top, 2)
            VStack(alignment: .leading, spacing: 3) {
                Text(tx.declineReason ?? "Autorisation refusée par MoneyPay.")
                    .font(.sub).foregroundStyle(Brand.ink)
                    .fixedSize(horizontal: false, vertical: true)
                Text("Un refus est facturé 220 FCFA par le processeur.")
                    .font(.micro).foregroundStyle(Brand.inkMuted)
            }
        }
        .padding(14)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Brand.debitSoft, in: .rect(cornerRadius: Metric.card, style: .continuous))
        .gutter()
        .padding(.top, 22)
    }

    /// Le décompte qui montre où part réellement l'argent.
    private var breakdown: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: "Décompte").gutter().padding(.top, 22).padding(.bottom, 6)
            VStack(spacing: 0) {
                if tx.amountUSDCents != 0 {
                    kv("Montant marchand", Fmt.usd(tx.amountUSDCents))
                    Rule()
                    kv("Taux appliqué", "1 USD = \(Int(tx.fxRate)) FCFA")
                    Rule()
                    kv("Contre-valeur", Fmt.xaf(gross))
                    Rule()
                    kv("Marge de change · \(Int(tx.fxMarginPct * 100)) %", Fmt.xaf(margin),
                       tint: Brand.pending)
                    Rule()
                }
                kv("Total débité", Fmt.xaf(abs(tx.amountXAF)), strong: true)
                Rule()
                kv("Catégorie", tx.category.label)
                Rule()
                Button {
                    UIPasteboard.general.string = tx.id.uuidString
                    Haptic.success(); toastMsg = Toast(text: "Référence copiée", icon: "doc.on.doc")
                } label: {
                    HStack {
                        Text("Référence").font(.body).foregroundStyle(Brand.inkMuted)
                        Spacer()
                        Text(String(tx.id.uuidString.prefix(13)))
                            .font(.dataMono).foregroundStyle(Brand.ink)
                        Image(systemName: "doc.on.doc").font(.system(size: 12))
                            .foregroundStyle(Brand.inkFaint)
                    }
                    .padding(.vertical, Metric.rowVertical)
                }
                .buttonStyle(.plain)
            }
            .gutter()
        }
        .padding(.bottom, 8)
    }

    private func kv(_ l: String, _ v: String, tint: Color = Brand.ink, strong: Bool = false) -> some View {
        HStack {
            Text(l).font(.body).foregroundStyle(Brand.inkMuted)
            Spacer()
            Text(v).font(strong ? .bodyMed : .body).foregroundStyle(tint).monospacedDigit()
        }
        .padding(.vertical, Metric.rowVertical)
    }

    private func paidWith(_ card: VirtualCard) -> some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: "Payé avec").gutter().padding(.top, 22).padding(.bottom, 2)
            NavigationLink { CardDetailView(card: card) } label: {
                HStack(spacing: 12) {
                    RoundedRectangle(cornerRadius: 5, style: .continuous)
                        .fill(card.theme.fill)
                        .frame(width: 34, height: 22)
                        .overlay { RoundedRectangle(cornerRadius: 5).stroke(Brand.hairline, lineWidth: 1) }
                    VStack(alignment: .leading, spacing: 2) {
                        Text(card.label).font(.body).foregroundStyle(Brand.ink)
                        Text("•• \(card.last4)").font(.sub).foregroundStyle(Brand.inkMuted)
                    }
                    Spacer()
                    Image(systemName: "chevron.right").font(.system(size: 13, weight: .semibold))
                        .foregroundStyle(Brand.inkFaint)
                }
                .padding(.vertical, Metric.rowVertical)
                .contentShape(.rect)
            }
            .buttonStyle(.plain)
            .gutter()
        }
        .padding(.bottom, 8)
    }

    private var actions: some View {
        VStack(spacing: 0) {
            Row(icon: "arrow.uturn.backward", title: "Contester ce paiement", chevron: true)
            Rule(inset: 51)
            Row(icon: "questionmark.circle", title: "Obtenir de l'aide", chevron: true)
            Rule(inset: 51)
            Row(icon: "square.and.arrow.up", title: "Partager le reçu", chevron: true)
        }
        .gutter()
        .padding(.top, 8)
    }
}
