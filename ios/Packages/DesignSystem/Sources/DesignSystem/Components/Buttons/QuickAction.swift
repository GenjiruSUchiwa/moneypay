import SwiftUI

/// Quick action (prototype `.qa`): a well disc with the label underneath. Every action
/// looks the same; the prototype gives none of them a coloured disc.
public struct QuickAction: View {
    public init(icon: String, label: Text, action: @escaping () -> Void = {}) {
        self.icon = icon
        self.label = label
        self.action = action
    }

    public var icon: String
    public var label: Text
    public var action: () -> Void = {}

    public var body: some View {
        Button(action: action) {
            VStack(spacing: 7) {
                Image(systemName: icon)
                    .font(.system(size: 17, weight: .medium))
                    .foregroundStyle(Brand.ink)
                    .frame(width: 46, height: 46)
                    .background(Brand.well, in: .circle)
                label
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
        QuickAction(icon: "plus", label: Text(verbatim: "Top up"))
        QuickAction(icon: "arrow.left.arrow.right", label: Text(verbatim: "Convert"))
        QuickAction(icon: "creditcard", label: Text(verbatim: "Cards"))
    }
    .padding()
    .page()
}
