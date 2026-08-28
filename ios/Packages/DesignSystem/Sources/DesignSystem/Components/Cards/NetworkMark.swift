import SwiftUI

/// Network mark. Mastercard as two discs, Visa as a wordmark.
public struct NetworkMark: View {
    public init(network: CardNetwork, ink: Color, scale: CGFloat = 1) {
        self.network = network
        self.ink = ink
        self.scale = scale
    }

    public var network: CardNetwork
    public var ink: Color
    public var scale: CGFloat = 1

    public var body: some View {
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

#Preview("NetworkMark") {
    HStack(spacing: 24) {
        NetworkMark(network: .visa, ink: Brand.ink)
        NetworkMark(network: .mastercard, ink: Brand.ink)
        NetworkMark(network: .mastercard, ink: Brand.ink, scale: 1.6)
    }
    .padding()
    .page()
}
