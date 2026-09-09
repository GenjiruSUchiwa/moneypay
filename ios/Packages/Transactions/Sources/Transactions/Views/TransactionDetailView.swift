import DesignSystem
import Money
import SwiftUI
import WalletStore

public struct TransactionDetailView: View {
    public init(tx: Money.Transaction) {
        self.tx = tx
    }

    public var tx: Money.Transaction
    @Environment(Store.self) private var store
    @State private var toastMsg: Toast?

    private var card: VirtualCard? { store.cards.first { $0.id == tx.cardID } }
    private var gross: Int { Int(Double(abs(tx.amountUSDCents)) / 100 * tx.fxRate) }
    private var margin: Int { max(0, abs(tx.amountXAF) - gross) }

    public var body: some View {
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
        .sensoryFeedback(.success, trigger: toastMsg) { _, new in new != nil }
    }

    private var header: some View {
        VStack(alignment: .leading, spacing: 0) {
            IconTile(symbol: tx.kind == .topUp ? "arrow.down" : tx.category.symbol,
                     tint: tx.kind == .topUp ? Brand.credit : tx.category.tint, size: 44)

            Text(verbatim: tx.merchant).font(.heading3).foregroundStyle(Brand.inkMuted).padding(.top, 16)

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
                StatusPill(text: Text(tx.status.label), symbol: tx.status.symbol,
                           tint: tx.status.tint, soft: tx.status.soft)
                Text(verbatim: Fmt.fullDate(tx.date)).font(.sub).foregroundStyle(Brand.inkMuted)
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
                Text(verbatim: tx.declineReason
                     ?? String(localized: "MoneyPay declined the authorization.", bundle: .module))
                    .font(.sub).foregroundStyle(Brand.ink)
                    .fixedSize(horizontal: false, vertical: true)
                Text("A decline costs \(Fmt.xaf(220)) from the processor.", bundle: .module)
                    .font(.micro).foregroundStyle(Brand.inkMuted)
            }
        }
        .padding(14)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Brand.debitSoft, in: .rect(cornerRadius: Metric.card, style: .continuous))
        .gutter()
        .padding(.top, 22)
    }

    private var breakdown: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: Text("Breakdown", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 6)
            VStack(spacing: 0) {
                if tx.amountUSDCents != 0 {
                    kv(Text("Merchant amount", bundle: .module),
                       Text(verbatim: Fmt.usd(tx.amountUSDCents)))
                    Rule()
                    kv(Text("Rate applied", bundle: .module),
                       Text(verbatim: "1 USD = \(Fmt.xaf(Int(tx.fxRate)))"))
                    Rule()
                    kv(Text("Equivalent", bundle: .module), Text(verbatim: Fmt.xaf(gross)))
                    Rule()
                    kv(Text("FX margin · \(tx.fxMarginPct, format: .percent)", bundle: .module),
                       Text(verbatim: Fmt.xaf(margin)), tint: Brand.pending)
                    Rule()
                }
                kv(Text("Total debited", bundle: .module),
                   Text(verbatim: Fmt.xaf(abs(tx.amountXAF))), strong: true)
                Rule()
                kv(Text("Category", bundle: .module), Text(tx.category.label))
                Rule()
                Button {
                    UIPasteboard.general.string = tx.id.uuidString
                    toastMsg = Toast(text: Text("Reference copied", bundle: .module), icon: "doc.on.doc")
                } label: {
                    HStack {
                        Text("Reference", bundle: .module).font(.bodyReg)
                            .foregroundStyle(Brand.inkMuted)
                        Spacer()
                        Text(verbatim: String(tx.id.uuidString.prefix(13)))
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

    private func kv(_ label: Text, _ value: Text, tint: Color = Brand.ink,
                    strong: Bool = false) -> some View {
        HStack {
            label.font(.bodyReg).foregroundStyle(Brand.inkMuted)
            Spacer()
            value.font(strong ? .bodyMed : .bodyReg).foregroundStyle(tint).monospacedDigit()
        }
        .padding(.vertical, Metric.rowVertical)
    }

    private func paidWith(_ card: VirtualCard) -> some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: Text("Paid with", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 2)
            Group {
                HStack(spacing: 12) {
                    RoundedRectangle(cornerRadius: 5, style: .continuous)
                        .fill(card.theme.fill)
                        .frame(width: 34, height: 22)
                        .overlay { RoundedRectangle(cornerRadius: 5).stroke(Brand.hairline, lineWidth: 1) }
                    VStack(alignment: .leading, spacing: 2) {
                        Text(verbatim: card.label).font(.bodyReg).foregroundStyle(Brand.ink)
                        Text(verbatim: "•• \(card.last4)").font(.sub).foregroundStyle(Brand.inkMuted)
                    }
                    Spacer()
                }
                .padding(.vertical, Metric.rowVertical)
            }
            .gutter()
        }
        .padding(.bottom, 8)
    }

    private var actions: some View {
        VStack(spacing: 0) {
            Row(icon: "arrow.uturn.backward",
                title: Text("Dispute this payment", bundle: .module), chevron: true)
            Rule(inset: 51)
            Row(icon: "questionmark.circle", title: Text("Get help", bundle: .module), chevron: true)
            Rule(inset: 51)
            Row(icon: "square.and.arrow.up",
                title: Text("Share the receipt", bundle: .module), chevron: true)
        }
        .gutter()
        .padding(.top, 8)
    }
}
