import ApiClient
import DesignSystem
import SwiftUI

/// Reuses the shipping screens with local state, without implying a successful authentication.
struct AuthenticationPreviewFlow: View {
    init(scenario: OnboardingRoot.PreviewScenario, onClose: @escaping () -> Void) {
        let model = SignUpModel(accounts: PreviewAccountClient())
        if scenario != .signInPhone { model.setPhoneDigits("699123456") }
        for _ in 0..<scenario.step.rawValue { model.advance() }
        if scenario == .verifying { model.draft.code = "482193" }
        if scenario.step == .profile {
            model.draft.firstName = "Alex"
            model.draft.lastName = "Morgan"
            model.draft.email = "alex@example.com"
        }
        _model = State(initialValue: model)
        _feedback = State(initialValue: scenario.feedback)
        _isSigningIn = State(initialValue: scenario.isSigningIn)
        self.onClose = onClose
    }

    let onClose: () -> Void
    @State private var model: SignUpModel
    @State private var feedback: AuthenticationFeedback?
    @State private var isSigningIn: Bool
    @Environment(\.accessibilityReduceMotion) private var reduceMotion

    var body: some View {
        NavigationStack {
            stepView
                .frame(maxWidth: .infinity, maxHeight: .infinity)
                .page()
                .toolbarTitleDisplayMode(.inline)
                .toolbar {
                    ToolbarItem(placement: .topBarLeading) {
                        Button(action: back) {
                            Label { Text("Back", bundle: .module) }
                            icon: { Image(systemName: "chevron.backward") }
                        }
                    }
                    ToolbarItem(placement: .principal) {
                        SegmentedProgress(count: isSigningIn ? 4 : 5, current: model.step.rawValue)
                            .frame(width: Metric.stage - Metric.gutter)
                    }
                }
        }
        .animation(reduceMotion ? nil : Motion.screen, value: model.step)
        .onChange(of: model.step) { _, step in
            feedback = nil
            if isSigningIn, step == .profile { onClose() }
        }
    }

    @ViewBuilder private var stepView: some View {
        switch model.step {
        case .phone:
            PhoneStepView(model: model, onContinue: advance, isSigningIn: isSigningIn,
                          feedback: feedback, onRecovery: recover)
        case .code:
            CodeStepView(model: model, feedback: feedback, onRecovery: recover)
        case .passcode:
            PasscodeStepView(model: model)
        case .biometrics:
            BiometricsStepView(model: model)
        case .profile:
            if !isSigningIn {
                ProfileStepView(model: model, onFinish: { _ in onClose() },
                                feedback: feedback, onRecovery: recover)
            }
        }
    }

    private func advance() {
        guard feedback?.isLoading != true else { return }
        feedback = nil
        model.advance()
    }

    private func back() {
        guard model.step != .phone else { onClose(); return }
        model.cancelVerification()
        feedback = nil
        model.back()
    }

    private func recover() {
        switch feedback {
        case .accountExists, .noAccount, .expiredSignUp:
            if feedback != .expiredSignUp { isSigningIn.toggle() }
            while model.step != .phone { model.back() }
        case .deliveryFailed, .expiredCode:
            model.draft.code = ""
            model.resend()
        case .emailExists:
            model.draft.email = ""
        default: break
        }
        feedback = nil
    }
}

#Preview("Sign-in — fr") {
    AuthenticationPreviewFlow(scenario: .signInPhone, onClose: {})
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Code delivery failure — dark") {
    AuthenticationPreviewFlow(scenario: .deliveryFailed, onClose: {})
        .environment(\.locale, Locale(identifier: "fr"))
        .preferredColorScheme(.dark)
}
