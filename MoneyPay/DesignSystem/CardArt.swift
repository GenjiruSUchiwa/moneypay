import SwiftUI

// MARK: - Habillages
//
// Des aplats, pas des dégradés. C'est ici — et seulement ici — que l'app
// prend de la couleur. Un dégradé à trois arrêts avec des courbes décoratives
// lit « maquette » ; un aplat franc avec un logotype lit « carte ».

enum CardTheme: String, CaseIterable, Identifiable, Codable {
    case ink, bone, pine, clay, slate, cobalt
    var id: String { rawValue }

    var fill: Color {
        switch self {
        case .ink:    Color(rgb: 0x15191B)
        case .bone:   Color(rgb: 0xE6E2D8)
        case .pine:   Color(rgb: 0x0E4F41)
        case .clay:   Color(rgb: 0xA34A32)
        case .slate:  Color(rgb: 0x3C4A55)
        case .cobalt: Color(rgb: 0x1C3C86)
        }
    }

    /// Encre lisible sur cet aplat.
    var ink: Color { self == .bone ? Color(rgb: 0x1A1712) : Color(rgb: 0xFAFAF8) }

    var label: String {
        switch self {
        case .ink: "Encre"; case .bone: "Ivoire"; case .pine: "Pin"
        case .clay: "Terre"; case .slate: "Ardoise"; case .cobalt: "Cobalt"
        }
    }
}

enum CardNetwork: String, Codable { case visa, mastercard }

// MARK: - Carte

struct VirtualCardView: View {
    var card: VirtualCard
    var revealed = false
    var compact = false

    private var ink: Color { card.theme.ink }
    private var pad: CGFloat { compact ? 15 : 22 }

    var body: some View {
        ZStack(alignment: .topLeading) {
            card.theme.fill

            // Un seul reflet, très bas en opacité : de la matière, pas un décor.
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
                Text("Gelée").font(.system(size: compact ? 11 : 13, weight: .medium))
            }
            .foregroundStyle(Brand.ink)
        }
        .clipShape(.rect(cornerRadius: compact ? 12 : 16, style: .continuous))
    }
}

/// Marque réseau. Mastercard en deux disques, Visa en logotype.
struct NetworkMark: View {
    var network: CardNetwork
    var ink: Color
    var scale: CGFloat = 1

    var body: some View {
        switch network {
        case .mastercard:
            HStack(spacing: -9 * scale) {
                Circle().fill(Color(rgb: 0xEB001B)).frame(width: 22 * scale, height: 22 * scale)
                Circle().fill(Color(rgb: 0xF79E1B)).frame(width: 22 * scale, height: 22 * scale)
                    .blendMode(.hardLight)
            }
        case .visa:
            Text("VISA")
                .font(.system(size: 15 * scale, weight: .heavy))
                .italic()
                .tracking(-0.3)
                .foregroundStyle(ink)
        }
    }
}

// MARK: - Monogramme

struct LogoMark: View {
    var size: CGFloat = 40
    var tint: Color = Brand.inkFill
    var glyph: Color = Brand.onInk

    var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: size * 0.24, style: .continuous).fill(tint)
            Path { p in
                p.move(to: .init(x: 0.24 * size, y: 0.72 * size))
                p.addLine(to: .init(x: 0.24 * size, y: 0.30 * size))
                p.addLine(to: .init(x: 0.50 * size, y: 0.56 * size))
                p.addLine(to: .init(x: 0.76 * size, y: 0.30 * size))
                p.addLine(to: .init(x: 0.76 * size, y: 0.72 * size))
            }
            .stroke(glyph, style: .init(lineWidth: size * 0.095, lineCap: .square, lineJoin: .miter))
        }
        .frame(width: size, height: size)
    }
}

struct Wordmark: View {
    var size: CGFloat = 18
    var body: some View {
        HStack(spacing: 8) {
            LogoMark(size: size * 1.35)
            Text("MoneyPay")
                .font(.system(size: size, weight: .semibold))
                .tight(-0.3)
                .foregroundStyle(Brand.ink)
        }
    }
}

extension String {
    func chunked(_ size: Int = 4, sep: String = " ") -> String {
        stride(from: 0, to: count, by: size).map {
            let s = index(startIndex, offsetBy: $0)
            let e = index(s, offsetBy: Swift.min(size, count - $0))
            return String(self[s..<e])
        }.joined(separator: sep)
    }
}
