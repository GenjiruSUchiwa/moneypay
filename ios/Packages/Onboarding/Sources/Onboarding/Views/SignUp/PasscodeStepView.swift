import ApiClient
import DesignSystem
import SwiftUI

struct PasscodeStepView: View {
    let model: SignUpModel

    var body: some View {
        VStack(spacing: 0) {
            VStack(spacing: Metric.lede) {
                Text(model.passcode.phase == .confirming
                     ? "Confirm your passcode" : "Create a passcode", bundle: .module)
                    .font(.titleLarge).tight().foregroundStyle(Brand.ink)
                    .accessibilityAddTraits(.isHeader)

                Text(model.passcode.isMismatch
                     ? "The passcodes do not match. Start again."
                     : "It unlocks the app and confirms your sensitive payments.", bundle: .module)
                    .font(.sub)
                    .foregroundStyle(model.passcode.isMismatch ? Brand.debit : Brand.inkMuted)
                    .multilineTextAlignment(.center)
                    .fixedSize(horizontal: false, vertical: true)
                    .frame(maxWidth: Metric.measure)
            }
            .padding(.top, Metric.block)

            passcodeInput
                .padding(.top, Metric.section)

            Spacer()
        }
        .gutter()
        .frame(maxWidth: .infinity)
        .sensoryFeedback(.success, trigger: model.lastPasscodeEvent) { _, new in new == .confirmed }
        .sensoryFeedback(.warning, trigger: model.lastPasscodeEvent) { _, new in new == .mismatch }
    }

    private var passcodeInput: some View {
        let phase = model.passcode.phase
        return PasscodeDots(code: Binding(get: { model.passcode.entry },
                                          set: { model.setPasscodeEntry($0, during: phase) }),
                            error: model.passcode.isMismatch)
            .id(phase)
    }
}

#Preview("Passcode — create, fr") {
    PasscodeStepView(model: SignUpModel(accounts: PreviewAccountClient()))
        .page()
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Passcode — mismatch, fr") {
    let model = SignUpModel(accounts: PreviewAccountClient())
    model.setPasscodeEntry("1234")
    model.setPasscodeEntry("5678")
    return PasscodeStepView(model: model)
        .page()
        .environment(\.locale, Locale(identifier: "fr"))
}
