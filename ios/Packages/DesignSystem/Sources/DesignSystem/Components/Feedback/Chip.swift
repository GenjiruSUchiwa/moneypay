import SwiftUI

public struct Chip: View {
    public init(text: String, selected: Bool = false, action: @escaping () -> Void = {}) {
        self.text = text
        self.selected = selected
        self.action = action
    }

    public var text: String
    public var selected = false
    public var action: () -> Void = {}

    public var body: some View {
        Button { Haptic.tap(); action() } label: {
            Text(text)
                .font(.system(size: 14, weight: .medium))
                .padding(.horizontal, 14).padding(.vertical, 8)
                .foregroundStyle(selected ? Brand.onInk : Brand.ink)
                .background(selected ? Brand.inkFill : Brand.well,
                            in: .rect(cornerRadius: 9, style: .continuous))
        }
        .buttonStyle(Press())
    }
}

#Preview("Chip") {
    HStack(spacing: 8) {
        Chip(text: "1 000")
        Chip(text: "5 000", selected: true)
        Chip(text: "10 000")
    }
    .padding()
    .page()
}
