import SwiftUI

/// Eyebrow: monospaced caps with widened tracking, the only form of capitals
/// the type rules allow here, and it reads like a bank statement.
public struct Eyebrow: View {
    public init(text: String) {
        self.text = text
    }

    public var text: String
    public var body: some View {
        Text(text.uppercased())
            .font(.eyebrow)
            .tracking(1.1)
            .foregroundStyle(Brand.inkFaint)
    }
}

#Preview("Eyebrow") {
    VStack(alignment: .leading, spacing: 12) {
        Eyebrow(text: "This month")
        Eyebrow(text: "Paid with")
    }
    .padding()
    .page()
}
