import SwiftUI

/// Trailing value of a row, in muted ink.
public struct RowValue: View {
    public init(text: String, tint: Color = Brand.inkMuted) {
        self.text = text
        self.tint = tint
    }

    public var text: String
    public var tint: Color = Brand.inkMuted
    public var body: some View { Text(text).font(.bodyReg).foregroundStyle(tint) }
}

#Preview("RowValue") {
    VStack(alignment: .trailing, spacing: 10) {
        RowValue(text: "428 500 FCFA")
        RowValue(text: "Frozen", tint: Brand.debit)
    }
    .padding()
    .page()
}
