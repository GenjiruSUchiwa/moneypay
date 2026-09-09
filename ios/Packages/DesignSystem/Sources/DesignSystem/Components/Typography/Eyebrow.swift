import SwiftUI

public struct Eyebrow: View {
    public init(text: Text) {
        self.text = text
    }

    public var text: Text
    public var body: some View {
        text
            .font(.eyebrow)
            .foregroundStyle(Brand.inkMuted)
    }
}

#Preview("Eyebrow") {
    VStack(alignment: .leading, spacing: 12) {
        Eyebrow(text: Text(verbatim: "≈ 3 minutes"))
        Eyebrow(text: Text(verbatim: "This month"))
    }
    .padding()
    .page()
}
