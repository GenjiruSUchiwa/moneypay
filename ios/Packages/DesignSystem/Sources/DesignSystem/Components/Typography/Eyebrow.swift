import SwiftUI

/// A short label above a title: "≈ 3 minutes", "This month". Prototype `.eyebrow`:
/// 13 pt medium, muted ink, no capitals. For raw data such as a PAN, use `Font.dataMono`.
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
