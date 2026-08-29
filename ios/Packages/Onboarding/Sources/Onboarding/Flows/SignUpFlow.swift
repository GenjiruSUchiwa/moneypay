import ApiClient
import DesignSystem
import Money
import SwiftUI

/// The sign-up shell: it owns the model, the navigation stack, the bar, the step switch and
/// the directional transition between steps. No other view in the package owns a step.
public struct SignUpFlow: View {
    /// Temporary bridge. `ios/App/RootView.swift` and `Gallery` → `ScreenCatalog` still build
    /// `SignUpFlow`; #38 routes both through `OnboardingRoot` and deletes this initialiser
    /// together with the `public` on the type.
    public init(startingAt step: SignUpStep = .phone, onDone: @escaping () -> Void) {
        self.init(accounts: PreviewAccountClient(), startingAt: step, onFinish: { _ in onDone() })
    }

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
    /// Which way the next step slides in. Set before the step changes, read by `stepTransition`.
    @State private var isAdvancing = true
    @Environment(\.accessibilityReduceMotion) private var reduceMotion

    public var body: some View {
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
        case .passcode: PasscodeStep(next: advance)      // replaced by #37
        case .biometrics: BiometricStep(next: advance)   // replaced by #37
        case .profile: ProfileStep(next: { onFinish(model.draft.user) })  // replaced by #37
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
    SignUpFlow(onDone: {})
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Sign-up flow — dark") {
    SignUpFlow(startingAt: .code, onDone: {})
        .environment(\.locale, Locale(identifier: "fr"))
        .preferredColorScheme(.dark)
}
