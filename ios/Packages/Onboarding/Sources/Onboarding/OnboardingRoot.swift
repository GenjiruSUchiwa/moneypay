import ApiClient
import DesignSystem
import Money
import SwiftUI

/// The single public entry point for the onboarding sequence; the app and Gallery
/// provide its account creator and receive one completion outcome.
public struct OnboardingRoot: View {
    /// The account creator selected by the composition root or a preview.
    public struct Dependencies: Sendable {
        public let accounts: any AccountCreating

        public init(accounts: any AccountCreating) {
            self.accounts = accounts
        }

        /// Uses the in-memory account creator so previews never require a server.
        public static var preview: Dependencies {
            Dependencies(accounts: PreviewAccountClient())
        }
    }

    /// The screen at which the sequence starts; Gallery uses this to open a step directly.
    public enum Stage: Equatable {
        case splash
        case welcome
        case signUp(SignUpStep)
    }

    /// Creates onboarding with the supplied account creator and completion handler.
    public init(
        dependencies: Dependencies,
        stage: Stage = .splash,
        onFinish: @escaping (OnboardingOutcome) -> Void
    ) {
        self.dependencies = dependencies
        self.onFinish = onFinish
        _stage = State(initialValue: stage)
    }

    private let dependencies: Dependencies
    private let onFinish: (OnboardingOutcome) -> Void
    @State private var stage: Stage

    public var body: some View {
        stageView
            .animation(Motion.screen, value: stage)
    }

    @ViewBuilder
    private var stageView: some View {
        switch stage {
        case .splash:
            SplashView { stage = .welcome }
        case .welcome:
            WelcomeView(
                onStart: { stage = .signUp(.phone) },
                onSignIn: { onFinish(.signedIn) }
            )
        case .signUp(let step):
            SignUpFlow(
                accounts: dependencies.accounts,
                startingAt: step,
                onFinish: { user in onFinish(.signedUp(user)) }
            )
        }
    }
}

#Preview("Onboarding — splash") {
    OnboardingRoot(dependencies: .preview, onFinish: { _ in })
}

#Preview("Onboarding — sign-up, fr") {
    OnboardingRoot(dependencies: .preview, stage: .signUp(.phone), onFinish: { _ in })
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Onboarding — dark") {
    OnboardingRoot(dependencies: .preview, stage: .welcome, onFinish: { _ in })
        .preferredColorScheme(.dark)
}
