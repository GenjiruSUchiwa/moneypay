import ApiClient
import DesignSystem
import SwiftUI

/// Step 1: the six-digit code. The boxes own the system number pad and SMS autofill;
/// the countdown and the verification both live on the model.
struct CodeStepView: View {
    let model: SignUpModel

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Enter the code", bundle: .module)
                .font(.heading1).tight().foregroundStyle(Brand.ink)
                .accessibilityAddTraits(.isHeader)

            Text("Sent to \(model.draft.displayPhone)", bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .monospacedDigit()
                .padding(.top, Metric.lede)

            OTPBoxes(code: Binding(get: { model.draft.code },
                                   set: { model.setCode($0) }),
                     length: SignUpDraft.codeLength)
                .padding(.top, Metric.section)

            resendRow
                .padding(.top, Metric.large)

            Spacer(minLength: Metric.block)

            MPButton(title: Text("Verify", bundle: .module),
                     tone: .primary,
                     loading: model.isVerifying,
                     enabled: model.draft.isCodeComplete) { model.verify() }
        }
        .gutter()
        .padding(.bottom, Metric.small)
        .task(id: model.resendGeneration) { await model.countdown() }
        .onDisappear { model.cancelVerification() }
    }

    @ViewBuilder private var resendRow: some View {
        if model.canResend {
            Button { model.resend() } label: {
                Text("Resend the code", bundle: .module)
                    .font(.subMed).foregroundStyle(Brand.mark)
            }
            .buttonStyle(Press())
        } else {
            Text("Resend the code in \(model.resendRemaining) s", bundle: .module)
                .font(.sub).foregroundStyle(Brand.inkMuted).monospacedDigit()
        }
    }
}

#Preview("Code step — fr") {
    let model = SignUpModel(accounts: PreviewAccountClient())
    model.setPhoneDigits("699123456")
    model.advance()
    return CodeStepView(model: model)
        .page()
        .environment(\.locale, Locale(identifier: "fr"))
}
