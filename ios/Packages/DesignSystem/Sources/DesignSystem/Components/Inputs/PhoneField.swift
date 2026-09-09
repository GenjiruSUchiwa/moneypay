import SwiftUI

public struct PhoneField: View {
    public init(
        flag: FlagMark.Country,
        dialCode: String,
        digits: Binding<String>,
        groupedDigits: String,
        placeholder: String,
        isValid: Bool,
        onCountryTap: @escaping () -> Void
    ) {
        self.flag = flag
        self.dialCode = dialCode
        self._digits = digits
        self.groupedDigits = groupedDigits
        self.placeholder = placeholder
        self.isValid = isValid
        self.onCountryTap = onCountryTap
    }

    private let flag: FlagMark.Country
    private let dialCode: String
    @Binding private var digits: String
    private let groupedDigits: String
    private let placeholder: String
    private let isValid: Bool
    private let onCountryTap: () -> Void

    public var body: some View {
        HStack(spacing: 0) {
            countryButton
            Rectangle().fill(Brand.rule).frame(width: 1, height: 22)
            numberField
        }
        .padding(.vertical, Metric.rowVertical)
        .overlay(alignment: .bottom) {
            Rectangle().fill(isValid ? Brand.ink : Brand.rule)
                .frame(height: isValid ? 1.5 : 1)
        }
        .animation(Motion.quick, value: isValid)
    }

    private var countryButton: some View {
        Button(action: onCountryTap) {
            HStack(spacing: 6) {
                FlagMark(flag, size: 24).accessibilityHidden(true)
                Text(verbatim: dialCode).font(.bodyMed).monospacedDigit()
                    .foregroundStyle(Brand.ink)
                Image(systemName: "chevron.down").font(.micro.weight(.bold))
                    .foregroundStyle(Brand.inkFaint)
            }
            .padding(.trailing, Metric.rowVertical)
            .contentShape(.rect)
        }
        .buttonStyle(Press())
        .accessibilityLabel(Text("Country code", bundle: .module))
        .accessibilityValue(Text(verbatim: dialCode))
    }

    private var numberField: some View {
        TextField(text: displayed, prompt: Text(verbatim: placeholder)) {
            Text(verbatim: placeholder)
        }
        .font(.input).monospacedDigit()
        .foregroundStyle(Brand.ink)
        .keyboardType(.phonePad)
        .textContentType(.telephoneNumber)
        .padding(.leading, Metric.rowVertical)
        .accessibilityLabel(Text("Phone number", bundle: .module))
    }

    private var displayed: Binding<String> {
        Binding(get: { groupedDigits }, set: { digits = $0.filter(\.isNumber) })
    }
}

#Preview("PhoneField") {
    @Previewable @State var empty = ""
    @Previewable @State var partial = "699"
    @Previewable @State var complete = "699123456"
    @Previewable @State var ivorian = ""
    VStack(spacing: Metric.large) {
        PhoneField(flag: .cm, dialCode: "+237", digits: $empty, groupedDigits: "",
                   placeholder: "6 XX XX XX XX", isValid: false, onCountryTap: {})
        PhoneField(flag: .cm, dialCode: "+237", digits: $partial, groupedDigits: "6 99",
                   placeholder: "6 XX XX XX XX", isValid: false, onCountryTap: {})
        PhoneField(flag: .cm, dialCode: "+237", digits: $complete,
                   groupedDigits: "6 99 12 34 56",
                   placeholder: "6 XX XX XX XX", isValid: true, onCountryTap: {})
        PhoneField(flag: .ci, dialCode: "+225", digits: $ivorian, groupedDigits: "",
                   placeholder: "X XX XX XX XXX", isValid: false, onCountryTap: {})
    }
    .gutter()
    .page()
}

#Preview("PhoneField — accessibility3") {
    @Previewable @State var complete = "699123456"
    PhoneField(flag: .cm, dialCode: "+237", digits: $complete,
               groupedDigits: "6 99 12 34 56",
               placeholder: "6 XX XX XX XX", isValid: true, onCountryTap: {})
        .gutter()
        .page()
        .environment(\.dynamicTypeSize, .accessibility3)
}
