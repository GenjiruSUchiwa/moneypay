import SwiftUI

/// The progress dots for a passcode entry.
/// Pass a `Binding` to own the system number pad; pass a count for a display-only
/// indicator. Use `OTPBoxes` for a code that can be autofilled — a passcode never is,
/// so this component deliberately offers no content type.
public struct PasscodeDots: View {
    /// Display-only dots. No keyboard.
    public init(filled: Int, total: Int = 4, error: Bool = false) {
        let slots = max(0, total)
        // The dots draw a count and never a digit, so any digit stands in for a filled slot.
        self._code = .constant(String(repeating: "0", count: min(max(0, filled), slots)))
        self.total = slots
        self.error = error
        self.autofocus = false
        self.editable = false
    }

    /// Editable dots. Owns the system number pad; the caller only holds the digits.
    /// `code` is clamped to `total` digits and never contains a non-digit.
    public init(code: Binding<String>, total: Int = 4, error: Bool = false, autofocus: Bool = true) {
        self._code = code
        self.total = max(0, total)
        self.error = error
        self.autofocus = autofocus
        self.editable = true
    }

    @Binding private var code: String
    private let total: Int
    private let error: Bool
    private let autofocus: Bool
    private let editable: Bool
    @FocusState private var keyboardFocused: Bool
    @Environment(\.accessibilityReduceMotion) private var reduceMotion

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
        dots
            .accessibilityElement(children: .ignore)
            .accessibilityLabel(Text("Passcode", bundle: .module))
            .accessibilityValue(progress)
    }

    // VoiceOver activates the field to type, so the announcement lives on the field.
    private var editableBody: some View {
        dots
            .accessibilityHidden(true)
            .overlay { field }
            .contentShape(.rect)
            .onTapGesture { keyboardFocused = true }
            .onAppear { if autofocus { keyboardFocused = true } }
    }

    private var dots: some View {
        HStack(spacing: 16) {
            ForEach(0..<total, id: \.self) { index in
                let isFilled = index < filled
                Circle()
                    .fill(isFilled ? (error ? Brand.debit : Brand.ink) : Brand.well)
                    .frame(width: 13, height: 13)
                    .scaleEffect(isFilled ? 1.06 : 1)
            }
        }
        .animation(Motion.quick, value: filled)
        .modifier(Shake(shakes: error && !reduceMotion ? 1 : 0))
        .animation(Motion.quick, value: error)
    }

    private var field: some View {
        TextField(text: sanitized, prompt: Text(verbatim: "")) {
            Text("Passcode", bundle: .module)
        }
        .keyboardType(.numberPad)
        .focused($keyboardFocused)
        .opacity(0)
        .accessibilityValue(progress)
    }

    private var digits: String { Self.sanitize(code, total: total) }

    private var filled: Int { digits.count }

    private var progress: Text {
        Text("\(filled) of \(total) digits entered", bundle: .module)
    }

    private var sanitized: Binding<String> {
        Binding(
            get: { digits },
            set: { code = Self.sanitize($0, total: total) }
        )
    }

    static func sanitize(_ raw: String, total: Int) -> String {
        String(raw.filter { $0.isASCII && $0.isNumber }.prefix(max(0, total)))
    }
}

public struct Shake: GeometryEffect {
    public init(shakes: CGFloat) {
        self.shakes = shakes
    }

    public var shakes: CGFloat
    public var animatableData: CGFloat { get { shakes } set { shakes = newValue } }
    public func effectValue(size: CGSize) -> ProjectionTransform {
        ProjectionTransform(CGAffineTransform(translationX: sin(shakes * .pi * 4) * 11, y: 0))
    }
}

#Preview("PasscodeDots") {
    @Previewable @State var entry = ""
    @Previewable @State var partial = "12"
    VStack(spacing: 26) {
        PasscodeDots(code: $entry, autofocus: false)
        PasscodeDots(code: $partial, autofocus: false)
        PasscodeDots(filled: 0)
        PasscodeDots(filled: 2)
        PasscodeDots(filled: 4, error: true)
        PasscodeDots(filled: 3, total: 6)
    }
    .padding()
    .page()
}
