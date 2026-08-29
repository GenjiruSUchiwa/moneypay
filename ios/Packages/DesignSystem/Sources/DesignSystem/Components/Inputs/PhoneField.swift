import SwiftUI

/// One line of phone entry: a country button, a hairline, and the national digits
/// (prototype `#signupPhone`). The country list, the dial codes and the digit grouping
/// belong to the feature that owns them — this component only displays what it is given.
///
/// For any other kind of text, use `Field`.
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
            TextField(text: displayed, prompt: Text(verbatim: placeholder)) {
                Text(verbatim: placeholder)
            }
            .font(.input)
            .monospacedDigit()
            .foregroundStyle(Brand.ink)
            .keyboardType(.phonePad)
            .textContentType(.telephoneNumber)
            .padding(.leading, Metric.rowVertical)
            .accessibilityLabel(Text("Phone number", bundle: .module))
        }
        .padding(.vertical, Metric.rowVertical)
        .overlay(alignment: .bottom) {
            Rectangle()
                .fill(isValid ? Brand.ink : Brand.rule)
                .frame(height: isValid ? 1.5 : 1)
        }
        .animation(Motion.quick, value: isValid)
    }

    private var countryButton: some View {
        Button {
            Haptic.tap()
            onCountryTap()
        } label: {
            HStack(spacing: 6) {
                FlagMark(flag, size: 24)
                    .accessibilityHidden(true)
                Text(verbatim: dialCode)
                    .font(.bodyMed)
                    .monospacedDigit()
                    .foregroundStyle(Brand.ink)
                Image(systemName: "chevron.down")
                    .font(.micro.weight(.bold))
                    .foregroundStyle(Brand.inkFaint)
            }
            .padding(.trailing, Metric.rowVertical)
            .contentShape(.rect)
        }
        .buttonStyle(Press())
        .accessibilityLabel(Text("Country code", bundle: .module))
        .accessibilityValue(Text(verbatim: dialCode))
    }

    private var displayed: Binding<String> {
        Binding(
            get: { groupedDigits },
            set: { digits = Self.nationalDigits(from: $0) }
        )
    }

    /// The field shows grouped digits; the caller only ever receives national digits.
    static func nationalDigits(from raw: String) -> String {
        raw.filter(\.isNumber)
    }
}

#Preview("PhoneField — states") {
    @Previewable @State var empty = ""
    @Previewable @State var partial = "69012"
    @Previewable @State var valid = "690123456"
    @Previewable @State var senegal = "771234567"
    VStack(spacing: Metric.large) {
        PhoneField(
            flag: .cm,
            dialCode: "+237",
            digits: $empty,
            groupedDigits: empty,
            placeholder: "6 XX XX XX XX",
            isValid: false,
            onCountryTap: {}
        )
        PhoneField(
            flag: .cm,
            dialCode: "+237",
            digits: $partial,
            groupedDigits: "6 90 12",
            placeholder: "6 XX XX XX XX",
            isValid: false,
            onCountryTap: {}
        )
        PhoneField(
            flag: .cm,
            dialCode: "+237",
            digits: $valid,
            groupedDigits: "6 90 12 34 56",
            placeholder: "6 XX XX XX XX",
            isValid: true,
            onCountryTap: {}
        )
        PhoneField(
            flag: .sn,
            dialCode: "+221",
            digits: $senegal,
            groupedDigits: senegal,
            placeholder: "X XX XX XX XX",
            isValid: false,
            onCountryTap: {}
        )
    }
    .gutter()
    .page()
}

#Preview("PhoneField — accessibility3") {
    @Previewable @State var digits = "690123456"
    PhoneField(
        flag: .cm,
        dialCode: "+237",
        digits: $digits,
        groupedDigits: "6 90 12 34 56",
        placeholder: "6 XX XX XX XX",
        isValid: true,
        onCountryTap: {}
    )
    .gutter()
    .page()
    .environment(\.dynamicTypeSize, .accessibility3)
}
