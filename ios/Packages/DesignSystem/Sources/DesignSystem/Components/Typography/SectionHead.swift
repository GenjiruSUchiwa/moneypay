import SwiftUI

/// Section heading. The chevron is the affordance: no button, no coloured
/// "See all" label.
public struct SectionHead: View {
    public init(title: Text, trailing: String? = nil, tappable: Bool = false,
                action: @escaping () -> Void = {}) {
        self.title = title
        self.trailing = trailing
        self.tappable = tappable
        self.action = action
    }

    public var title: Text
    public var trailing: String? = nil
    public var tappable = false
    public var action: () -> Void = {}

    public var body: some View {
        Button {
            if tappable { action() }
        } label: {
            HStack(alignment: .firstTextBaseline, spacing: 6) {
                title.font(.heading2).tight().foregroundStyle(Brand.ink)
                if tappable {
                    Image(systemName: "chevron.forward")
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
        SectionHead(title: Text(verbatim: "Cards"))
        SectionHead(title: Text(verbatim: "Activity"), trailing: "12", tappable: true)
    }
    .gutter()
    .page()
}
