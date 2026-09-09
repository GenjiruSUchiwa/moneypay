import SwiftUI

public struct AmountEntry: View {
    public init(digits: String, currency: String, size: CGFloat = 46) {
        self._digits = .constant(digits)
        self.display = digits
        self.currency = currency
        self.size = size
        self.maxDigits = 0
        self.autofocus = false
        self.editable = false
    }

    public init(digits: Binding<String>, display: String, currency: String, size: CGFloat = 46,
                maxDigits: Int = 8, autofocus: Bool = true) {
        self._digits = digits
        self.display = display
        self.currency = currency
        self.size = size
        self.maxDigits = max(0, maxDigits)
        self.autofocus = autofocus
        self.editable = true
    }

    @Binding private var digits: String
    private let display: String
    private let currency: String
    private let size: CGFloat
    private let maxDigits: Int
    private let autofocus: Bool
    private let editable: Bool

    @State private var blink = true
    @FocusState private var keyboardFocused: Bool

    public var body: some View {
        Group {
            if editable {
                editableBody
            } else {
                amount
            }
        }
    }

    private var editableBody: some View {
        amount
            .accessibilityHidden(true)
            .overlay { field }
            .contentShape(.rect)
            .onTapGesture { keyboardFocused = true }
            .onAppear { if autofocus { keyboardFocused = true } }
    }

    private var amount: some View {
        HStack(alignment: .firstTextBaseline, spacing: 3) {
            Text(verbatim: displayText)
                .font(.system(size: size, weight: .semibold))
                .tracking(-1)
                .foregroundStyle(digits.isEmpty ? Brand.inkFaint : Brand.ink)
                .monospacedDigit()
                .contentTransition(.numericText())
            Rectangle()
                .fill(Brand.ink)
                .frame(width: 2, height: size * 0.78)
                .opacity(blink ? 1 : 0)
            Text(verbatim: currency)
                .font(.system(size: size * 0.4, weight: .medium))
                .foregroundStyle(Brand.inkMuted)
        }
        .animation(.spring(response: 0.22, dampingFraction: 0.85), value: display)
        .task {
            while !Task.isCancelled {
                try? await Task.sleep(for: .milliseconds(560))
                blink.toggle()
            }
        }
    }

    private var field: some View {
        TextField(text: sanitized, prompt: Text(verbatim: "")) {
            Text("Amount", bundle: .module)
        }
        .keyboardType(.numberPad)
        .focused($keyboardFocused)
        .opacity(0)
        .accessibilityValue(Text(verbatim: "\(displayText) \(currency)"))
    }

    private var displayText: String { display.isEmpty ? "0" : display }

    private var sanitized: Binding<String> {
        Binding(
            get: { Self.sanitize(digits, maxDigits: maxDigits) },
            set: { digits = Self.sanitize($0, maxDigits: maxDigits) }
        )
    }

    static func sanitize(_ raw: String, maxDigits: Int) -> String {
        String(raw.filter { $0.isASCII && $0.isNumber }.prefix(max(0, maxDigits)))
    }
}

#Preview("AmountEntry") {
    @Previewable @State var entry = ""
    @Previewable @State var typed = "25000"
    VStack(spacing: 30) {
        AmountEntry(digits: $entry, display: Fmt.group(Int(entry) ?? 0), currency: "FCFA",
                    autofocus: false)
        AmountEntry(digits: $typed, display: Fmt.group(Int(typed) ?? 0), currency: "FCFA",
                    autofocus: false)
        AmountEntry(digits: "", currency: "FCFA")
        AmountEntry(digits: "25 000", currency: "FCFA")
        AmountEntry(digits: "12.50", currency: "USD", size: 34)
    }
    .padding()
    .page()
}
