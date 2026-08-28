import SwiftUI

/// Quick action: a quiet tile with the label underneath. No coloured disc.
public struct QuickAction: View {
    public init(icon: String, label: String, emphasis: Bool = false, action: @escaping () -> Void = {}) {
        self.icon = icon
        self.label = label
        self.emphasis = emphasis
        self.action = action
    }

    public var icon: String
    public var label: String
    public var emphasis = false
    public var action: () -> Void = {}

    public var body: some View {
        Button { Haptic.tap(); action() } label: {
            VStack(spacing: 7) {
                Image(systemName: icon)
                    .font(.system(size: 17, weight: .medium))
                    .foregroundStyle(emphasis ? Brand.onInk : Brand.ink)
                    .frame(width: 46, height: 46)
                    .background(emphasis ? Brand.inkFill : Brand.well, in: .circle)
                Text(label)
                    .font(.micro)
                    .foregroundStyle(Brand.inkMuted)
                    .lineLimit(1).minimumScaleFactor(0.8)
            }
            .frame(maxWidth: .infinity)
        }
        .buttonStyle(Press())
    }
}

#Preview("QuickAction") {
    HStack(spacing: 22) {
        QuickAction(icon: "plus", label: "Top up", emphasis: true)
        QuickAction(icon: "arrow.left.arrow.right", label: "Convert")
        QuickAction(icon: "creditcard", label: "Cards")
    }
    .padding()
    .page()
}
