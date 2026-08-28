import DesignSystem
import SwiftUI

public struct VirtualCardView: View {
    public init(card: VirtualCard, revealed: Bool = false, compact: Bool = false) {
        self.card = card
        self.revealed = revealed
        self.compact = compact
    }

    public var card: VirtualCard
    public var revealed = false
    public var compact = false

    private var ink: Color { card.theme.ink }
    private var pad: CGFloat { compact ? 15 : 22 }

    public var body: some View {
        ZStack(alignment: .topLeading) {
            card.theme.fill

            // A single sheen at very low opacity: material, not decoration.
            LinearGradient(colors: [ink.opacity(0.07), .clear],
                           startPoint: .topTrailing, endPoint: .center)

            content.padding(pad)
        }
        .aspectRatio(1.586, contentMode: .fit)          // ratio ISO/IEC 7810 ID-1
        .clipShape(.rect(cornerRadius: compact ? 12 : 16, style: .continuous))
        .overlay {
            RoundedRectangle(cornerRadius: compact ? 12 : 16, style: .continuous)
                .stroke(ink.opacity(0.10), lineWidth: 1)
        }
        .overlay { if card.isFrozen { frozen } }
    }

    private var content: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack(alignment: .top) {
                HStack(spacing: 6) {
                    RoundedRectangle(cornerRadius: 2.5, style: .continuous)
                        .fill(ink)
                        .frame(width: compact ? 9 : 11, height: compact ? 9 : 11)
                    Text("MoneyPay")
                        .font(.system(size: compact ? 12 : 14, weight: .semibold))
                        .foregroundStyle(ink)
                }
                Spacer()
                Text("virtuelle")
                    .font(.system(size: compact ? 9 : 10, weight: .medium, design: .monospaced))
                    .tracking(0.6)
                    .foregroundStyle(ink.opacity(0.55))
            }

            Spacer(minLength: 0)

            Text(card.label)
                .font(.system(size: compact ? 13 : 16, weight: .medium))
                .foregroundStyle(ink)
                .lineLimit(1)

            Text(revealed ? card.pan.chunked() : "•• \(card.last4)")
                .font(.system(size: compact ? 12 : 14, weight: .regular, design: .monospaced))
                .tracking(compact ? 0.4 : 1.0)
                .foregroundStyle(ink.opacity(0.72))
                .padding(.top, compact ? 3 : 5)
                .contentTransition(.numericText())

            HStack(alignment: .bottom, spacing: 18) {
                if revealed && !compact {
                    secret("expire", card.expiry)
                    secret("cvv", card.cvv)
                }
                Spacer(minLength: 0)
                NetworkMark(network: card.network, ink: ink, scale: compact ? 0.72 : 1)
            }
            .padding(.top, compact ? 8 : 14)
        }
    }

    private func secret(_ label: String, _ value: String) -> some View {
        VStack(alignment: .leading, spacing: 2) {
            Text(label)
                .font(.system(size: 9, weight: .medium, design: .monospaced))
                .tracking(0.6)
                .foregroundStyle(ink.opacity(0.5))
            Text(value)
                .font(.system(size: 13, weight: .regular, design: .monospaced))
                .foregroundStyle(ink)
        }
    }

    private var frozen: some View {
        ZStack {
            Rectangle().fill(Brand.bg.opacity(0.72))
            VStack(spacing: 6) {
                Image(systemName: "snowflake").font(.system(size: compact ? 16 : 20, weight: .medium))
                Text("Frozen", bundle: .module).font(.system(size: compact ? 11 : 13, weight: .medium))
            }
            .foregroundStyle(Brand.ink)
        }
        .clipShape(.rect(cornerRadius: compact ? 12 : 16, style: .continuous))
    }
}
