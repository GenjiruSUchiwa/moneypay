import ApiClient
import Money
import Observation

/// The whole sign-up flow's state and timing. Views read it and call intents; they own no
/// timers and no business rules. Haptics stay in the views.
@Observable
final class SignUpModel {
    /// Prototype `every(1000)` counting 42 s down (`signupOTP`).
    let resendDelay: Duration
    /// Prototype `after(700)` (`ACTIONS.otpVerify`). The POC does not check the code, so the
    /// wait *is* the verification.
    let verifyDelay: Duration
    /// One countdown tick. A parameter only so tests do not spend 42 seconds of wall clock.
    let resendTick: Duration

    private(set) var step: SignUpStep = .phone
    var draft = SignUpDraft()
    private(set) var passcode = PasscodeEntry()
    /// The last outcome of the passcode entry, for the view's haptic (`.onChange`). Reset to
    /// `nil` on the next edit so two mismatches in a row both fire.
    private(set) var lastPasscodeEvent: PasscodeEntry.Event?
    private(set) var resendRemaining = 0
    private(set) var isVerifying = false
    private(set) var isSubmitting = false
    private(set) var submissionFailed = false
    /// Bumped whenever the resend clock is re-armed. The view runs one `countdown()` per
    /// generation with `.task(id: resendGeneration)`, exactly as `WelcomeModel` re-arms its
    /// dwell, so a resend restarts a whole delay instead of racing the old one.
    private(set) var resendGeneration = 0
    /// Internal, not private, so a test can `await model.verification?.value` instead of sleeping.
    @ObservationIgnored private(set) var verification: Task<Void, Never>?

    var canResend: Bool { resendRemaining == 0 }

    @ObservationIgnored private let accounts: any AccountCreating

    init(accounts: any AccountCreating,
         resendDelay: Duration = .seconds(42),
         verifyDelay: Duration = .milliseconds(700),
         resendTick: Duration = .seconds(1)) {
        self.accounts = accounts
        self.resendDelay = resendDelay
        self.verifyDelay = verifyDelay
        self.resendTick = resendTick
    }

    // MARK: - Navigation

    func advance() {
        guard let next = SignUpStep(rawValue: step.rawValue + 1) else { return }
        step = next
        if next == .code { armResend() }
    }

    func back() {
        guard let previous = SignUpStep(rawValue: step.rawValue - 1) else { return }
        step = previous
    }

    // MARK: - Phone step

    /// Prototype `pickCountry`: a shorter country cannot keep a longer number.
    func selectCountry(_ country: Country) {
        draft.country = country
        draft.phoneDigits = String(draft.phoneDigits.prefix(country.digitCount))
    }

    func setPhoneDigits(_ raw: String) {
        draft.phoneDigits = String(raw.filter(\.isNumber).prefix(draft.country.digitCount))
    }

    // MARK: - Code step

    func setCode(_ raw: String) {
        draft.code = String(raw.filter(\.isNumber).prefix(SignUpDraft.codeLength))
        if draft.isCodeComplete { verify() }
    }

    /// Idempotent: the sixth digit and the Verify button both call this, and the prototype's
    /// `if (F.verifying) return` is the reason the second call must do nothing.
    func verify() {
        guard !isVerifying, draft.isCodeComplete else { return }
        isVerifying = true
        verification = Task { await self.runVerification() }
    }

    func cancelVerification() {
        verification?.cancel()
        verification = nil
        isVerifying = false
    }

    /// One tick per `resendTick` while the clock still runs. Drive it with
    /// `.task(id: resendGeneration)` so a resend replaces the running countdown.
    func countdown() async {
        while resendRemaining > 0, !Task.isCancelled {
            try? await Task.sleep(for: resendTick)
            guard !Task.isCancelled else { return }
            resendRemaining -= 1
        }
    }

    /// Re-arms the clock only. The POC has no resend endpoint, exactly like `ACTIONS.otpResend`.
    func resend() { armResend() }

    // MARK: - Passcode step

    func setPasscodeEntry(_ raw: String) {
        lastPasscodeEvent = passcode.setEntry(raw)
        if lastPasscodeEvent == .confirmed {
            draft.passcode = passcode.first
            advance()
        }
    }

    // MARK: - Biometrics step

    func chooseBiometrics(_ enabled: Bool) {
        draft.biometricsEnabled = enabled
        advance()
    }

    // MARK: - Profile step

    /// Creates the account and hands back the locally built user **either way**: the POC degrades
    /// silently when the sandbox is down (prototype `goKYC` → `liveFail`), and a sign-up must not
    /// dead-end on an unreachable server. The returned `UserStateDTO.id` is dropped on purpose —
    /// nothing stores a session yet (Sessions epic).
    func submit() async -> User {
        guard !isSubmitting else { return draft.user }
        isSubmitting = true
        submissionFailed = false
        defer { isSubmitting = false }
        do {
            _ = try await accounts.createAccount(draft.signupRequest)
        } catch {
            // Never log the error or the request: it carries the full phone number.
            submissionFailed = true
        }
        return draft.user
    }

    // MARK: - Private

    private func runVerification() async {
        try? await Task.sleep(for: verifyDelay)
        isVerifying = false
        guard !Task.isCancelled else { return }
        advance()
    }

    private func armResend() {
        resendRemaining = Int(resendDelay.components.seconds)
        resendGeneration += 1
    }
}
