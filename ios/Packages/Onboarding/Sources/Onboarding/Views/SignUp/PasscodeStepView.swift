import ApiClient
import DesignSystem
import SwiftUI

/// Step 2: create a four-digit passcode, then repeat it. `PasscodeDots` owns the system
/// number pad and the mismatch shake; the two phases, the comparison and the advance all
/// live on the model.
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

            // A text field hands over its whole current text on every edit; `setPasscodeEntry`
            // owns the digits, the length and the phase, so this view never sees a keystroke.
            PasscodeDots(code: Binding(get: { model.passcode.entry },
                                       set: { model.setPasscodeEntry($0) }),
                         error: model.passcode.isMismatch)
                .padding(.top, Metric.section)

            Spacer()
        }
        .gutter()
        .frame(maxWidth: .infinity)
        .sensoryFeedback(.success, trigger: model.lastPasscodeEvent) { _, new in new == .confirmed }
        .sensoryFeedback(.warning, trigger: model.lastPasscodeEvent) { _, new in new == .mismatch }
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
