extension OnboardingRoot {
    public enum PreviewScenario: String, CaseIterable, Sendable {
        case signInPhone = "signin-phone"
        case signInCode = "signin-code"
        case signInPasscode = "signin-passcode"
        case signInBiometrics = "signin-faceid"
        case sending = "phone-sending"
        case connectionFailed = "phone-offline"
        case queued = "code-queued"
        case verifying = "code-verifying"
        case deliveryFailed = "code-delivery-failed"
        case invalidCode = "code-invalid"
        case locked = "code-locked"
        case expiredCode = "code-expired"
        case resendTooSoon = "code-resend-wait"
        case resendLimit = "code-resend-limit"
        case expiredSignUp = "signup-expired"
        case accountExists = "phone-registered"
        case noAccount = "phone-unregistered"
        case submitting = "profile-submitting"
        case completionFailed = "profile-failed"
        case emailExists = "profile-email-used"

        var step: SignUpStep {
            switch self {
            case .signInPhone, .sending, .connectionFailed, .expiredSignUp: .phone
            case .signInPasscode: .passcode
            case .signInBiometrics: .biometrics
            case .submitting, .completionFailed, .emailExists: .profile
            default: .code
            }
        }

        var isSigningIn: Bool {
            [.signInPhone, .signInCode, .signInPasscode, .signInBiometrics, .noAccount].contains(self)
        }

        var feedback: AuthenticationFeedback? {
            Self.feedbacks[self]
        }

        private static let feedbacks: [Self: AuthenticationFeedback] = [
            .sending: .sending, .connectionFailed: .connectionFailed,
            .queued: .queued, .verifying: .verifying, .deliveryFailed: .deliveryFailed,
            .invalidCode: .invalidCode(attemptsRemaining: 3),
            .locked: .locked, .expiredCode: .expiredCode, .resendTooSoon: .resendTooSoon(seconds: 42),
            .resendLimit: .resendLimit, .expiredSignUp: .expiredSignUp,
            .accountExists: .accountExists, .noAccount: .noAccount,
            .submitting: .submitting, .completionFailed: .completionFailed, .emailExists: .emailExists
        ]
    }
}
