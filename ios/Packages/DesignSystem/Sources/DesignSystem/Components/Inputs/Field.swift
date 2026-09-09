import SwiftUI

public struct Field: View {
    public init(
        placeholder: Text,
        text: Binding<String>,
        icon: String? = nil,
        keyboard: UIKeyboardType = .default,
        contentType: UITextContentType? = nil,
        submit: SubmitLabel = .next,
        focused: Bool = false
    ) {
        self.placeholder = placeholder
        self._text = text
        self.icon = icon
        self.keyboard = keyboard
        self.contentType = contentType
        self.submit = submit
        self.focused = focused
    }

    public var placeholder: Text
    @Binding public var text: String
    public var icon: String? = nil
    public var keyboard: UIKeyboardType = .default
    public var contentType: UITextContentType? = nil
    public var submit: SubmitLabel = .next
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
                .textContentType(contentType)
                .submitLabel(submit)
                .textInputAutocapitalization(keyboard == .emailAddress ? .never : .sentences)
                .autocorrectionDisabled()
        }
        .padding(.horizontal, 14).frame(height: 50)
        .inputChrome(emphasized: focused)
        .animation(Motion.quick, value: focused)
    }
}

#Preview("Field") {
    @Previewable @State var empty = ""
    @Previewable @State var filled = "Aristide"
    @Previewable @State var email = ""
    VStack(spacing: 12) {
        Field(placeholder: Text(verbatim: "Full name"), text: $empty)
        Field(placeholder: Text(verbatim: "Full name"), text: $filled, icon: "person")
        Field(placeholder: Text(verbatim: "Phone"), text: $empty, icon: "phone", keyboard: .phonePad)
        Field(placeholder: Text(verbatim: "Email address"), text: $email, icon: "envelope",
              keyboard: .emailAddress, contentType: .emailAddress, submit: .done, focused: true)
    }
    .gutter()
    .page()
}
