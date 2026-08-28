import SwiftUI

/// Trailing value of a row, in muted ink.
public struct RowValue: View {
    public init(text: Text, tint: Color = Brand.inkMuted) {
        self.text = text
        self.tint = tint
    }

    public var text: Text
    public var tint: Color = Brand.inkMuted
    public var body: some View { text.font(.bodyReg).foregroundStyle(tint) }
}

#Preview("RowValue") {
    VStack(alignment: .trailing, spacing: 10) {
        RowValue(text: Text(verbatim: "428 500 FCFA"))
        RowValue(text: Text(verbatim: "Frozen"), tint: Brand.debit)
    }
    .padding()
    .page()
}
