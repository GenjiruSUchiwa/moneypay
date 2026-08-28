import SwiftUI

/// Lightweight navigation bar, no chrome.
public struct NavBar: View {
    public init(title: String = "", onBack: (() -> Void)? = nil, onClose: (() -> Void)? = nil) {
        self.title = title
        self.onBack = onBack
        self.onClose = onClose
    }

    public var title: String = ""
    public var onBack: (() -> Void)? = nil
    public var onClose: (() -> Void)? = nil

    public var body: some View {
        HStack {
            if let onBack {
                Button { Haptic.tap(); onBack() } label: {
                    Image(systemName: "chevron.left")
                        .font(.system(size: 17, weight: .semibold))
                        .foregroundStyle(Brand.ink)
                        .frame(width: 40, height: 40)
                        .contentShape(.rect)
                }
                .padding(.leading, -10)
            }
            Spacer()
            Text(title).font(.heading3).foregroundStyle(Brand.ink)
            Spacer()
            if let onClose {
                Button { Haptic.tap(); onClose() } label: {
                    Image(systemName: "xmark")
                        .font(.system(size: 15, weight: .semibold))
                        .foregroundStyle(Brand.inkMuted)
                        .frame(width: 40, height: 40)
                        .contentShape(.rect)
                }
                .padding(.trailing, -10)
            } else if onBack != nil {
                Color.clear.frame(width: 40, height: 40)
            }
        }
        .gutter()
        .frame(height: 48)
    }
}

#Preview("NavBar") {
    VStack(spacing: 20) {
        NavBar(title: "Top up", onBack: {})
        NavBar(title: "New card", onClose: {})
        NavBar(title: "Both", onBack: {}, onClose: {})
    }
    .page()
}
