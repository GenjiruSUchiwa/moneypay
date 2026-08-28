import SwiftUI

public struct Field: View {
    public init(
        placeholder: Text,
        text: Binding<String>,
        icon: String? = nil,
        keyboard: UIKeyboardType = .default,
        focused: Bool = false
    ) {
        self.placeholder = placeholder
        self._text = text
        self.icon = icon
        self.keyboard = keyboard
        self.focused = focused
    }

    public var placeholder: Text
    @Binding public var text: String
    public var icon: String? = nil
    public var keyboard: UIKeyboardType = .default
    public var focused: Bool = false

    public var body: some View {
        HStack(spacing: 11) {
            if let icon {
                Image(systemName: icon).font(.system(size: 15))
                    .foregroundStyle(Brand.inkFaint).frame(width: 18)
            }
            TextField(text: $text, prompt: placeholder) { placeholder }
                .font(.bodyReg).foregroundStyle(Brand.ink)
                .keyboardType(keyboard)
                .textInputAutocapitalization(keyboard == .emailAddress ? .never : .sentences)
                .autocorrectionDisabled()
        }
        .padding(.horizontal, 14).frame(height: 50)
        .background(Brand.well, in: .rect(cornerRadius: Metric.control, style: .continuous))
        .overlay {
            RoundedRectangle(cornerRadius: Metric.control)
                .stroke(focused ? Brand.ink : .clear, lineWidth: 1.2)
        }
    }
}

#Preview("Field") {
    @Previewable @State var empty = ""
    @Previewable @State var filled = "Aristide"
    VStack(spacing: 12) {
        Field(placeholder: Text(verbatim: "Full name"), text: $empty)
        Field(placeholder: Text(verbatim: "Full name"), text: $filled, icon: "person")
        Field(placeholder: Text(verbatim: "Phone"), text: $empty, icon: "phone", keyboard: .phonePad)
    }
    .gutter()
    .page()
}
