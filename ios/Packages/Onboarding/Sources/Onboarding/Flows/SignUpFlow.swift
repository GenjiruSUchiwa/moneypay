import ApiClient
import DesignSystem
import Money
import SwiftUI

struct SignUpFlow: View {
    init(accounts: any AccountCreating,
         startingAt step: SignUpStep = .phone,
         onFinish: @escaping (User) -> Void) {
        let model = SignUpModel(accounts: accounts)
        // The model owns its step; walking it forward is the only way in, and it arms the
        // resend clock exactly as reaching the code step normally would.
        for _ in 0..<step.rawValue { model.advance() }
        _model = State(initialValue: model)
        self.onFinish = onFinish
    }

    private let onFinish: (User) -> Void
    @State private var model: SignUpModel
    @State private var isAdvancing = true
    @Environment(\.accessibilityReduceMotion) private var reduceMotion

    var body: some View {
        // The stack has no path and no destinations: it exists to host the system bar.
        NavigationStack {
            ZStack {
                stepView.transition(stepTransition)
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
            .page()
            .toolbarTitleDisplayMode(.inline)
            .toolbar {
                if model.step != .phone {
                    ToolbarItem(placement: .topBarLeading) {
                        Button {
                            isAdvancing = false
                            model.back()
                        } label: {
                            Label { Text("Back", bundle: .module) }
                            icon: { Image(systemName: "chevron.backward") }
                        }
                    }
                }
                ToolbarItem(placement: .principal) {
                    // The segments are capsules with no intrinsic width and the principal slot
                    // proposes none, so the bar needs one. A stage minus a gutter clears the
                    // leading slot, which is what keeps the bar still when Back appears.
                    SegmentedProgress(count: SignUpStep.allCases.count, current: model.step.rawValue)
                        .frame(width: Metric.stage - Metric.gutter)
                }
            }
        }
        // `.animation(_:value:)` rather than `withAnimation`: the model advances itself from
        // `verify()` and from the passcode entry, outside any closure this view controls.
        .animation(reduceMotion ? nil : Motion.screen, value: model.step)
    }

    @ViewBuilder private var stepView: some View {
        switch model.step {
        case .phone: PhoneStepView(model: model, onContinue: advance)
        case .code: CodeStepView(model: model)
        case .passcode: PasscodeStepView(model: model)
        case .biometrics: BiometricsStepView(model: model)
        case .profile: ProfileStepView(model: model, onFinish: onFinish)
        }
    }

    private var stepTransition: AnyTransition {
        guard !reduceMotion else { return .opacity }
        let insertionEdge: Edge = isAdvancing ? .trailing : .leading
        let removalEdge: Edge = isAdvancing ? .leading : .trailing
        return .asymmetric(
            insertion: .move(edge: insertionEdge).combined(with: .opacity),
            removal: .move(edge: removalEdge).combined(with: .opacity)
        )
    }

    private func advance() {
        isAdvancing = true
        model.advance()
    }
}

#Preview("Sign-up flow — fr") {
    SignUpFlow(accounts: PreviewAccountClient(), onFinish: { _ in })
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Sign-up flow — dark") {
    SignUpFlow(accounts: PreviewAccountClient(), startingAt: .code, onFinish: { _ in })
        .environment(\.locale, Locale(identifier: "fr"))
        .preferredColorScheme(.dark)
}
