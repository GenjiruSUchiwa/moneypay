import SwiftUI

/// Success check. One ink disc, not a burst of concentric circles.
public struct SuccessMark: View {
    public init() {
    }

    @State private var on = false
    public var body: some View {
        Image(systemName: "checkmark")
            .font(.system(size: 26, weight: .semibold))
            .foregroundStyle(Brand.onInk)
            .frame(width: 56, height: 56)
            .background(Brand.credit, in: .circle)
            .scaleEffect(on ? 1 : 0.7)
            .opacity(on ? 1 : 0)
            .task { withAnimation(Motion.settle) { on = true } }
    }
}

#Preview("SuccessMark") {
    SuccessMark().padding().page()
}
