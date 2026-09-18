import Foundation

public struct PreviewSignUpClient: SignUpRegistering {
    private let started: Result<SignUpStarted, SignUpError>
    private let state: Result<SignUpState, SignUpError>
    private let resent: Result<SignUpState, SignUpError>
    private let verified: Result<PhoneVerified, SignUpError>
    private let delay: Duration

    public init(
        started: Result<SignUpStarted, SignUpError> = .success(.sample),
        state: Result<SignUpState, SignUpError> = .success(.sample),
        resent: Result<SignUpState, SignUpError> = .success(.sample),
        verified: Result<PhoneVerified, SignUpError> = .success(.sample),
        delay: Duration = .zero
    ) {
        self.started = started
        self.state = state
        self.resent = resent
        self.verified = verified
        self.delay = delay
    }

    public func start(phone: String, termsVersion: String, privacyVersion: String) async throws(SignUpError) -> SignUpStarted {
        try await answer(started)
    }

    public func state(of signUp: SignUpStarted) async throws(SignUpError) -> SignUpState {
        try await answer(state)
    }

    public func resendCode(for signUp: SignUpStarted) async throws(SignUpError) -> SignUpState {
        try await answer(resent)
    }

    public func verifyPhone(for signUp: SignUpStarted, code: String) async throws(SignUpError) -> PhoneVerified {
        try await answer(verified)
    }

    private func answer<Value>(_ result: Result<Value, SignUpError>) async throws(SignUpError) -> Value {
        if delay > .zero {
            try? await Task.sleep(for: delay)
        }
        return try result.get()
    }
}

public extension SignUpStarted {
    static let sample = SignUpStarted(
        signUpId: "1f7d854a-3996-4a6b-8cc8-1fe3e83bc205",
        signUpToken: "preview-signup-token",
        codeDelivery: .queued,
        codeExpiresAt: .sample(minutes: 5),
        canResendAt: .sample(minutes: 1),
        signUpExpiresAt: .sample(minutes: 15)
    )
}

public extension SignUpState {
    static let sample = SignUpState(
        signUpId: SignUpStarted.sample.signUpId,
        status: .codePending,
        codeDelivery: .sent,
        codeExpiresAt: .sample(minutes: 5),
        canResendAt: .sample(minutes: 1),
        signUpExpiresAt: .sample(minutes: 15)
    )
}

public extension PhoneVerified {
    static let sample = PhoneVerified(
        signUpId: SignUpStarted.sample.signUpId,
        registrationToken: "preview-registration-token",
        signUpExpiresAt: .sample(minutes: 15)
    )
}

private extension Date {
    static func sample(minutes: Int) -> Date {
        Date(timeIntervalSince1970: 1_788_030_000 + TimeInterval(minutes * 60))
    }
}
