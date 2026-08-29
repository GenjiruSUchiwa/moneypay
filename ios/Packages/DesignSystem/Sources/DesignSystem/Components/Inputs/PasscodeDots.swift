import SwiftUI

/// The progress dots for a passcode entry.
/// Use this for a short passcode indicator; use `OTPBoxes` when the code can be autofilled.
public struct PasscodeDots: View {
    public init(filled: Int, total: Int = 4, error: Bool = false) {
        self.filled = filled
        self.total = total
        self.error = error
    }

    public var filled: Int
    public var total: Int = 4
    public var error = false
    @Environment(\.accessibilityReduceMotion) private var reduceMotion

    public var body: some View {
        HStack(spacing: 16) {
            ForEach(0..<total, id: \.self) { index in
                let isFilled = index < filled
                Circle()
                    .fill(isFilled ? (error ? Brand.debit : Brand.ink) : Brand.well)
                    .frame(width: 13, height: 13)
                    .scaleEffect(isFilled ? 1.06 : 1)
            }
        }
        .animation(Motion.quick, value: filled)
        .modifier(Shake(shakes: error && !reduceMotion ? 1 : 0))
        .animation(Motion.quick, value: error)
        .accessibilityElement(children: .ignore)
        .accessibilityLabel(Text("Passcode", bundle: .module))
        .accessibilityValue(Text("\(filled) of \(total) digits entered", bundle: .module))
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
