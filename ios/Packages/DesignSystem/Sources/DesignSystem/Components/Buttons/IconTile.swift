import SwiftUI

public struct IconTile: View {
    public init(symbol: String, tint: Color = Brand.ink, size: CGFloat = 38, filled: Bool = false) {
        self.symbol = symbol
        self.tint = tint
        self.size = size
        self.filled = filled
    }

    public var symbol: String
    public var tint: Color = Brand.ink
    public var size: CGFloat = 38
    public var filled = false

    public var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: size * 0.29, style: .continuous)
                .fill(filled ? tint : Brand.well)
            Image(systemName: symbol)
                .font(.system(size: size * 0.42, weight: .medium))
                .foregroundStyle(filled ? Brand.onAction : tint)
        }
        .frame(width: size, height: size)
    }
}

#Preview("IconTile") {
    HStack(spacing: 14) {
        IconTile(symbol: "creditcard.fill")
        IconTile(symbol: "arrow.down.circle.fill", tint: Brand.credit)
        IconTile(symbol: "lock.fill", tint: Brand.mark, filled: true)
        IconTile(symbol: "bell.fill", size: 30)
    }
    .padding()
    .page()
}
