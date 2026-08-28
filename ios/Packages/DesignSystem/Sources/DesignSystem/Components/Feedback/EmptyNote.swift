import SwiftUI

public struct EmptyNote: View {
    public init(title: String, message: String, actionTitle: String? = nil, action: @escaping () -> Void = {}) {
        self.title = title
        self.message = message
        self.actionTitle = actionTitle
        self.action = action
    }

    public var title: String
    public var message: String
    public var actionTitle: String? = nil
    public var action: () -> Void = {}

    public var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(title).font(.bodyMed).foregroundStyle(Brand.ink)
            Text(message).font(.sub).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
            if let actionTitle {
                Button(action: action) {
                    HStack(spacing: 4) {
                        Text(actionTitle).font(.subMed)
                        Image(systemName: "arrow.right").font(.system(size: 11, weight: .bold))
                    }
                    .foregroundStyle(Brand.mark)
                }
                .padding(.top, 4)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(.vertical, 18)
    }
}

#Preview("EmptyNote") {
    VStack(spacing: 40) {
        EmptyNote(title: "No card yet", message: "Create a virtual card to pay online.")
        EmptyNote(title: "No transaction",
                  message: "Your payments will show up here.",
                  actionTitle: "Top up")
    }
    .page()
}
