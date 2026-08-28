import SwiftUI

/// Eyebrow: monospaced caps with widened tracking, the only form of capitals
/// the type rules allow here, and it reads like a bank statement.
public struct Eyebrow: View {
    public init(text: Text) {
        self.text = text
    }

    public var text: Text
    public var body: some View {
        text
            .textCase(.uppercase)
            .font(.eyebrow)
            .tracking(1.1)
            .foregroundStyle(Brand.inkFaint)
    }
}

#Preview("Eyebrow") {
    VStack(alignment: .leading, spacing: 12) {
        Eyebrow(text: Text(verbatim: "This month"))
        Eyebrow(text: Text(verbatim: "Paid with"))
    }
    .padding()
    .page()
}
