import SwiftUI

public struct StatusPill: View {
    public init(text: Text, symbol: String, tint: Color, soft: Color) {
        self.text = text
        self.symbol = symbol
        self.tint = tint
        self.soft = soft
    }

    public var text: Text
    public var symbol: String
    public var tint: Color
    public var soft: Color

    public var body: some View {
        HStack(spacing: 4) {
            Image(systemName: symbol).font(.system(size: 9, weight: .bold))
            text.font(.system(size: 11, weight: .medium))
        }
        .padding(.horizontal, 7).padding(.vertical, 3)
        .foregroundStyle(tint)
        .background(soft, in: .rect(cornerRadius: 5, style: .continuous))
    }
}

#Preview("StatusPill") {
    VStack(alignment: .leading, spacing: 10) {
        StatusPill(text: Text(verbatim: "Approved"), symbol: "checkmark",
                   tint: Brand.credit, soft: Brand.creditSoft)
        StatusPill(text: Text(verbatim: "Pending"), symbol: "clock",
                   tint: Brand.pending, soft: Brand.pendingSoft)
        StatusPill(text: Text(verbatim: "Declined"), symbol: "xmark",
                   tint: Brand.debit, soft: Brand.debitSoft)
    }
    .padding()
    .page()
}
