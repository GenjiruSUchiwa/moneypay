import DesignSystem
import Money
import SwiftUI

public struct TransactionRow: View {
    public init(tx: Money.Transaction) {
        self.tx = tx
    }

    public var tx: Money.Transaction

    public var body: some View {
        HStack(spacing: 12) {
            IconTile(symbol: tx.kind == .topUp ? "arrow.down" : tx.category.symbol, tint: tint)

            VStack(alignment: .leading, spacing: 2) {
                HStack(spacing: 6) {
                    Text(verbatim: tx.merchant).font(.bodyReg).foregroundStyle(Brand.ink).lineLimit(1)
                    if tx.status != .approved {
                        StatusPill(text: Text(tx.status.label), symbol: tx.status.symbol,
                                   tint: tx.status.tint, soft: tx.status.soft)
                    }
                }
                Text("\(String(localized: tx.kind.label)) · \(Fmt.time(tx.date))", bundle: .module)
                    .font(.sub).foregroundStyle(Brand.inkMuted).lineLimit(1)
            }
            Spacer(minLength: 10)

            VStack(alignment: .trailing, spacing: 2) {
                if tx.status == .declined {
                    Text(verbatim: "—").font(.bodyMed).foregroundStyle(Brand.inkFaint)
                } else {
                    MoneyText.xaf(tx.amountXAF, size: 16, weight: .medium,
                                  color: tx.amountXAF > 0 ? Brand.credit : Brand.ink,
                                  signed: true, unit: false)
                }
                if tx.amountUSDCents != 0 {
                    Text(verbatim: Fmt.usd(tx.amountUSDCents)).font(.micro).foregroundStyle(Brand.inkFaint)
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
