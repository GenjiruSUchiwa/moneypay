import ApiClient
import DesignSystem
import SwiftUI

struct PhoneStepView: View {
    let model: SignUpModel
    let onContinue: () -> Void
    var isSigningIn = false
    var feedback: AuthenticationFeedback?
    var onRecovery: () -> Void = {}
    @State private var isPickingCountry = false

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text(isSigningIn ? "Welcome back" : "What is your\nnumber?", bundle: .module)
                .font(.heading1).tight().foregroundStyle(Brand.ink)
                .accessibilityAddTraits(.isHeader)

            Text(isSigningIn
                 ? "Enter your account's phone number. We will send you a six-digit code to sign in."
                 : "A six-digit code goes out to this line. It is also the line your Mobile Money top-ups arrive on.",
                 bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
                .padding(.top, Metric.lede)

            PhoneField(
                flag: model.draft.country.flag,
                dialCode: model.draft.country.dialCode,
                digits: Binding(get: { model.draft.phoneDigits },
                                set: { model.setPhoneDigits($0) }),
                groupedDigits: model.draft.country.grouped(model.draft.phoneDigits),
                placeholder: model.draft.country.placeholder,
                isValid: model.draft.isPhoneValid,
                onCountryTap: { isPickingCountry = true }
            )
            .padding(.top, Metric.block)
            .disabled(feedback?.isLoading == true)

            if let feedback {
                AuthenticationFeedbackView(feedback: feedback, onRecovery: onRecovery)
            }

            Spacer(minLength: Metric.block)

            MPButton(title: Text("Send me the code", bundle: .module),
                     tone: .primary,
                     loading: feedback?.isLoading == true,
                     enabled: model.draft.isPhoneValid,
                     action: onContinue)

            Text(isSigningIn
                 ? "On a new device, you will set up a new passcode and Face ID after verification."
                 : "By continuing you accept MoniPay's terms and privacy policy.", bundle: .module)
                .font(.micro).foregroundStyle(Brand.inkFaint)
                .fixedSize(horizontal: false, vertical: true)
                .padding(.top, Metric.small)
        }
        .gutter()
        .padding(.bottom, Metric.small)
        .sheet(isPresented: $isPickingCountry) {
            CountryPickerView(selected: model.draft.country) { model.selectCountry($0) }
                .presentationDetents([.medium])
        }
    }
}

#Preview("Phone step — fr") {
    PhoneStepView(model: SignUpModel(accounts: PreviewAccountClient()), onContinue: {})
        .page()
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Phone step — fr, XXL") {
    PhoneStepView(model: SignUpModel(accounts: PreviewAccountClient()), onContinue: {})
        .page()
        .environment(\.locale, Locale(identifier: "fr"))
        .environment(\.dynamicTypeSize, .accessibility3)
}
