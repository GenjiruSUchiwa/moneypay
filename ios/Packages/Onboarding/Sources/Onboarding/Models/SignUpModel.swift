import ApiClient
import Money
import Observation

@Observable
final class SignUpModel {
    let resendDelay: Duration
    let verifyDelay: Duration
    let resendTick: Duration

    private(set) var step: SignUpStep = .phone
    var draft = SignUpDraft()
    private(set) var passcode = PasscodeEntry()
    private(set) var lastPasscodeEvent: PasscodeEntry.Event?
    private(set) var resendRemaining = 0
    private(set) var isVerifying = false
    private(set) var isSubmitting = false
    private(set) var submissionFailed = false
    private(set) var resendGeneration = 0
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

    func advance() {
        guard let next = SignUpStep(rawValue: step.rawValue + 1) else { return }
        step = next
        if next == .code { armResend() }
    }

    func back() {
        guard let previous = SignUpStep(rawValue: step.rawValue - 1) else { return }
        if previous == .passcode {
            passcode = PasscodeEntry()
            draft.passcode = ""
            lastPasscodeEvent = nil
        }
        step = previous
    }

    func selectCountry(_ country: Country) {
        draft.country = country
        draft.phoneDigits = String(draft.phoneDigits.prefix(country.digitCount))
    }

    func setPhoneDigits(_ raw: String) {
        draft.phoneDigits = String(raw.filter(\.isNumber).prefix(draft.country.digitCount))
    }

    func setCode(_ raw: String) {
        draft.code = String(raw.filter(\.isNumber).prefix(SignUpDraft.codeLength))
        if draft.isCodeComplete { verify() }
    }

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

    func countdown() async {
        while resendRemaining > 0, !Task.isCancelled {
            try? await Task.sleep(for: resendTick)
            guard !Task.isCancelled else { return }
            resendRemaining -= 1
        }
    }

    func resend() { armResend() }

    func setPasscodeEntry(_ raw: String, during phase: PasscodeEntry.Phase) {
        guard step == .passcode, passcode.phase == phase else { return }
        setPasscodeEntry(raw)
    }

    func setPasscodeEntry(_ raw: String) {
        lastPasscodeEvent = passcode.setEntry(raw)
        if lastPasscodeEvent == .confirmed {
            draft.passcode = passcode.first
            advance()
        }
    }

    func chooseBiometrics(_ enabled: Bool) {
        draft.biometricsEnabled = enabled
        advance()
    }

    func submit() async -> User {
        guard !isSubmitting else { return draft.user }
        isSubmitting = true
        submissionFailed = false
        defer { isSubmitting = false }
        do {
            _ = try await accounts.createAccount(draft.signupRequest)
        } catch {
            submissionFailed = true
        }
        return draft.user
    }

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
