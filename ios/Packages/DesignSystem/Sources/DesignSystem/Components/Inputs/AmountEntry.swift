import SwiftUI

/// Amount entry: the digits so far plus a caret, currency set as a suffix.
public struct AmountEntry: View {
    public init(digits: String, currency: String, size: CGFloat = 46) {
        self.digits = digits
        self.currency = currency
        self.size = size
    }

    public var digits: String
    public var currency: String
    public var size: CGFloat = 46

    @State private var blink = true

    public var body: some View {
        HStack(alignment: .firstTextBaseline, spacing: 3) {
            Text(digits.isEmpty ? "0" : digits)
                .font(.system(size: size, weight: .semibold))
                .tracking(-1)
                .foregroundStyle(digits.isEmpty ? Brand.inkFaint : Brand.ink)
                .monospacedDigit()
                .contentTransition(.numericText())
            Rectangle()
                .fill(Brand.ink)
                .frame(width: 2, height: size * 0.78)
                .opacity(blink ? 1 : 0)
            Text(currency)
                .font(.system(size: size * 0.4, weight: .medium))
                .foregroundStyle(Brand.inkMuted)
        }
        .animation(.spring(response: 0.22, dampingFraction: 0.85), value: digits)
        .task {
            while !Task.isCancelled {
                try? await Task.sleep(for: .milliseconds(560))
                blink.toggle()
            }
        }
    }
}

#Preview("AmountEntry") {
    VStack(spacing: 30) {
        AmountEntry(digits: "", currency: "FCFA")
        AmountEntry(digits: "25000", currency: "FCFA")
        AmountEntry(digits: "12.50", currency: "USD", size: 34)
    }
    .padding()
    .page()
}
