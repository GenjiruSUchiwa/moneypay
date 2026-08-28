import SwiftUI

/// Section heading. The chevron is the affordance: no button, no coloured
/// "See all" label.
public struct SectionHead: View {
    public init(title: String, trailing: String? = nil, tappable: Bool = false, action: @escaping () -> Void = {}) {
        self.title = title
        self.trailing = trailing
        self.tappable = tappable
        self.action = action
    }

    public var title: String
    public var trailing: String? = nil
    public var tappable = false
    public var action: () -> Void = {}

    public var body: some View {
        Button {
            if tappable { Haptic.tap(); action() }
        } label: {
            HStack(alignment: .firstTextBaseline, spacing: 6) {
                Text(title).font(.heading2).tight().foregroundStyle(Brand.ink)
                if tappable {
                    Image(systemName: "chevron.right")
                        .font(.system(size: 13, weight: .semibold))
                        .foregroundStyle(Brand.inkFaint)
                        .baselineOffset(-1)
                }
                Spacer(minLength: 8)
                if let trailing {
                    Text(trailing).font(.sub).foregroundStyle(Brand.inkMuted)
                }
            }
            .contentShape(.rect)
        }
        .buttonStyle(.plain)
        .disabled(!tappable)
    }
}

#Preview("SectionHead") {
    VStack(alignment: .leading, spacing: 24) {
        SectionHead(title: "Cards")
        SectionHead(title: "Activity", trailing: "12", tappable: true)
    }
    .gutter()
    .page()
}
