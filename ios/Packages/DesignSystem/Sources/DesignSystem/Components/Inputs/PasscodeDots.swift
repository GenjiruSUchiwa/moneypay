import SwiftUI

/// Dots of a passcode.
public struct PasscodeDots: View {
    public init(filled: Int, total: Int = 4, error: Bool = false) {
        self.filled = filled
        self.total = total
        self.error = error
    }

    public var filled: Int
    public var total: Int = 4
    public var error = false

    public var body: some View {
        HStack(spacing: 16) {
            ForEach(0..<total, id: \.self) { i in
                Circle()
                    .fill(i < filled ? (error ? Brand.debit : Brand.ink) : .clear)
                    .frame(width: 12, height: 12)
                    .overlay { Circle().stroke(i < filled ? .clear : Brand.rule, lineWidth: 1.2) }
                    .animation(.spring(response: 0.2, dampingFraction: 0.6), value: filled)
            }
        }
        .modifier(Shake(shakes: error ? 1 : 0))
        .animation(.default, value: error)
    }
}

public struct Shake: GeometryEffect {
    public init(shakes: CGFloat) {
        self.shakes = shakes
    }

    public var shakes: CGFloat
    public var animatableData: CGFloat { get { shakes } set { shakes = newValue } }
    public func effectValue(size: CGSize) -> ProjectionTransform {
        ProjectionTransform(CGAffineTransform(translationX: sin(shakes * .pi * 4) * 11, y: 0))
    }
}

#Preview("PasscodeDots") {
    VStack(spacing: 26) {
        PasscodeDots(filled: 0)
        PasscodeDots(filled: 2)
        PasscodeDots(filled: 4, error: true)
        PasscodeDots(filled: 3, total: 6)
    }
    .padding()
    .page()
}
