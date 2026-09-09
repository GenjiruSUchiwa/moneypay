import ApiClient
import DesignSystem
import SwiftUI

struct BiometricsStepView: View {
    let model: SignUpModel

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()

            Image(systemName: "faceid")
                .font(.system(size: Metric.heroGlyph, weight: .light))
                .foregroundStyle(Brand.ink)
                .accessibilityHidden(true)

            Text("Use Face ID?", bundle: .module)
                .font(.heading1).tight().foregroundStyle(Brand.ink)
                .accessibilityAddTraits(.isHeader)
                .padding(.top, Metric.large)

            Text("Open the app and confirm payments without typing your passcode.", bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
                .padding(.top, Metric.lede)

            HStack(alignment: .top, spacing: Metric.stack) {
                Image(systemName: "lock.shield").font(.sub).foregroundStyle(Brand.inkFaint)
                Text("Your biometric data never leaves your iPhone.", bundle: .module)
                    .font(.sub).foregroundStyle(Brand.inkMuted)
                    .fixedSize(horizontal: false, vertical: true)
            }
            .padding(.top, Metric.large)

            Spacer()

            VStack(spacing: Metric.stack) {
                MPButton(title: Text("Turn on Face ID", bundle: .module), tone: .primary) {
                    model.chooseBiometrics(true)
                }
                MPButton(title: Text("Later", bundle: .module), tone: .ghost) {
                    model.chooseBiometrics(false)
                }
            }
        }
        .gutter()
        .padding(.bottom, Metric.rowVertical)
    }
}

#Preview("Biometrics — fr") {
    BiometricsStepView(model: SignUpModel(accounts: PreviewAccountClient()))
        .page()
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Biometrics — fr, XXL") {
    BiometricsStepView(model: SignUpModel(accounts: PreviewAccountClient()))
        .page()
        .environment(\.locale, Locale(identifier: "fr"))
        .environment(\.dynamicTypeSize, .accessibility3)
}
