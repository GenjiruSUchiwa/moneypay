import SwiftUI

/// The slots of a one-time code, as boxes.
/// Pass a `Binding` to own the system keyboard and SMS autofill; pass a `String`
/// for a display-only indicator (for example while a custom keypad still drives
/// the digits). Use `Field` for free-form text.
public struct OTPBoxes: View {
    /// Display-only slots. No keyboard.
    public init(code: String, length: Int = 6) {
        self._code = .constant(Self.sanitize(code, length: length))
        self.length = max(0, length)
        self.autofocus = false
        self.editable = false
    }

    /// Editable slots. Owns the system keyboard and one-time-code autofill.
    public init(code: Binding<String>, length: Int = 6, autofocus: Bool = true) {
        self._code = code
        self.length = max(0, length)
        self.autofocus = autofocus
        self.editable = true
    }

    @Binding private var code: String
    private let length: Int
    private let autofocus: Bool
    private let editable: Bool
    @FocusState private var keyboardFocused: Bool

    public var body: some View {
        Group {
            if editable {
                editableBody
            } else {
                displayBody
            }
        }
    }

    private var displayBody: some View {
        boxes
            .accessibilityElement(children: .ignore)
            .accessibilityLabel(Text("Verification code", bundle: .module))
            .accessibilityValue(progress)
    }

    private var editableBody: some View {
        boxes
            .accessibilityHidden(true)
            .overlay { field }
            .onAppear { if autofocus { keyboardFocused = true } }
    }

    private var boxes: some View {
        HStack(spacing: 8) {
            ForEach(0..<length, id: \.self) { index in
                box(at: index)
            }
        }
        .animation(Motion.quick, value: digits)
    }

    private func box(at index: Int) -> some View {
        let characters = Array(digits)
        let digit = index < characters.count ? String(characters[index]) : ""
        let isCursor = index == digits.count && (!editable || keyboardFocused)

        return Text(verbatim: digit)
            .font(.input)
            .monospacedDigit()
            .foregroundStyle(Brand.ink)
            .contentTransition(.numericText())
            .frame(maxWidth: .infinity)
            .frame(height: 56)
            .inputChrome(emphasized: isCursor)
    }

    private var field: some View {
        TextField(text: sanitized, prompt: Text(verbatim: "")) {
            Text("Verification code", bundle: .module)
        }
        .keyboardType(.numberPad)
        .textContentType(.oneTimeCode)
        .focused($keyboardFocused)
        .opacity(0)
        .accessibilityValue(progress)
    }

    private var digits: String { Self.sanitize(code, length: length) }

    private var progress: Text {
        Text("\(digits.count) of \(length) digits entered", bundle: .module)
    }

    private var sanitized: Binding<String> {
        Binding(
            get: { digits },
            set: { code = Self.sanitize($0, length: length) }
        )
    }

    static func sanitize(_ raw: String, length: Int) -> String {
        String(raw.filter { $0.isASCII && $0.isNumber }.prefix(max(0, length)))
    }
}

#Preview("OTPBoxes") {
    @Previewable @State var empty = ""
    @Previewable @State var partial = "4821"
    @Previewable @State var full = "482193"
    VStack(spacing: 26) {
        OTPBoxes(code: $empty, autofocus: false)
        OTPBoxes(code: $partial, autofocus: false)
        OTPBoxes(code: $full, autofocus: false)
        OTPBoxes(code: "4821")
    }
    .padding()
    .page()
}
