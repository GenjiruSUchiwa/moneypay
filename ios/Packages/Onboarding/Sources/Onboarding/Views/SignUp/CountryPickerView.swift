import DesignSystem
import SwiftUI

/// The supported-country sheet of the phone step. The system owns the sheet's material:
/// nothing here sets a presentation background.
struct CountryPickerView: View {
    let selected: Country
    let onSelect: (Country) -> Void
    @Environment(\.dismiss) private var dismiss

    /// The prototype's 30 pt `.fl-flag`, composed from tokens.
    private static let flagSize = Metric.gutter + Metric.stack

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Country", bundle: .module)
                .font(.heading2).tight().foregroundStyle(Brand.ink)
                .gutter()
                .padding(.top, Metric.gutter)
                .padding(.bottom, Metric.small)
            Rule()
            ScrollView {
                RuledStack(data: Country.supported,
                           inset: Self.flagSize + Metric.control) { country in
                    row(for: country)
                }
                .gutter()
            }
        }
    }

    private func row(for country: Country) -> some View {
        Button {
            onSelect(country)
            dismiss()
        } label: {
            HStack(spacing: Metric.control) {
                FlagMark(country.flag, size: Self.flagSize)
                Row(title: Text(verbatim: country.localizedName)) {
                    RowValue(text: Text(verbatim: country.dialCode))
                        .monospacedDigit()
                        .accessibilityLabel(Text("Country code", bundle: .module))
                    Image(systemName: "checkmark")
                        .font(.microMed.weight(.bold))
                        .foregroundStyle(Brand.ink)
                        .opacity(country == selected ? 1 : 0)
                        .accessibilityHidden(true)
                }
            }
            .contentShape(.rect)
        }
        .buttonStyle(.plain)
        .accessibilityAddTraits(country == selected ? .isSelected : [])
    }
}

#Preview("Country picker — fr") {
    CountryPickerView(selected: .cameroon, onSelect: { _ in })
        .environment(\.locale, Locale(identifier: "fr"))
}
