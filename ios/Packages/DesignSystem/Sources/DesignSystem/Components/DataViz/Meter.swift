import SwiftUI

/// Thin gauge. Two pixels: it informs without adding weight.
public struct Meter: View {
    public init(value: Double, tint: Color = Brand.ink, height: CGFloat = 4) {
        self.value = value
        self.tint = tint
        self.height = height
    }

    public var value: Double
    public var tint: Color = Brand.ink
    public var height: CGFloat = 4

    public var body: some View {
        GeometryReader { geo in
            ZStack(alignment: .leading) {
                Capsule().fill(Brand.hairline)
                Capsule().fill(tint)
                    .frame(width: max(0.02, min(1, value)) * geo.size.width)
            }
        }
        .frame(height: height)
    }
}

#Preview("Meter") {
    VStack(spacing: 16) {
        Meter(value: 0.15)
        Meter(value: 0.62, tint: Brand.credit)
        Meter(value: 0.95, tint: Brand.debit, height: 8)
    }
    .gutter()
    .page()
}
