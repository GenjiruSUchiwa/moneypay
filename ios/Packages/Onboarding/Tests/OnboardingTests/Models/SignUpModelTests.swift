import ApiClient
import Testing
@testable import Onboarding

@Suite("SignUpModel")
struct SignUpModelTests {
    private static func model(resendDelay: Duration = .seconds(42),
                              verifyDelay: Duration = .zero,
                              resendTick: Duration = .zero,
                              result: Result<UserStateDTO, ApiError> = .success(PreviewAccountClient.sampleUser))
        -> SignUpModel {
        SignUpModel(accounts: PreviewAccountClient(result: result),
                    resendDelay: resendDelay,
                    verifyDelay: verifyDelay,
                    resendTick: resendTick)
    }

    private static func profileModel(result: Result<UserStateDTO, ApiError>) -> SignUpModel {
        let signUp = model(result: result)
        signUp.setPhoneDigits("699123456")
        signUp.draft.firstName = " Aristide "
        signUp.draft.lastName = "Mbassi"
        signUp.draft.email = "aristide@example.cm "
        return signUp
    }

    @Test("back() at the phone step is a no-op")
    func backAtTheFirstStep() {
        let model = Self.model()

        model.back()

        #expect(model.step == .phone)
    }

    @Test("advance() stops at the profile step")
    func advanceStopsAtTheLastStep() {
        let model = Self.model()

        for _ in 0..<SignUpStep.allCases.count + 2 { model.advance() }

        #expect(model.step == .profile)
    }

    @Test("Choosing a shorter country trims the digits already typed")
    func shorterCountryTrimsDigits() throws {
        let model = Self.model()
        model.setPhoneDigits("699123456")
        let gabon = try #require(Country.supported.first { $0.id == "GA" })

        model.selectCountry(gabon)

        #expect(model.draft.phoneDigits == "69912345")
        #expect(model.draft.isPhoneValid)
    }

    @Test("Non-digits are stripped and the number is clamped to the country's length")
    func phoneDigitsAreStrippedAndClamped() {
        let model = Self.model()

        model.setPhoneDigits("+237 6a9 91-2345678")

        #expect(model.draft.phoneDigits == "237699123")
    }

    @Test("The sixth digit starts verification, and a second call is ignored")
    func verificationIsIdempotent() async {
        let model = Self.model()
        model.advance()

        model.setCode("699123")
        model.verify()
        await model.verification?.value

        #expect(model.step == .passcode)
    }

    @Test("Verification lands on the passcode step and clears the loading flag")
    func verificationClearsTheLoadingFlag() async {
        let model = Self.model()
        model.advance()

        model.setCode("699123")
        #expect(model.isVerifying)
        await model.verification?.value

        #expect(model.isVerifying == false)
        #expect(model.step == .passcode)
    }

    @Test("Arriving on the code step arms the resend clock")
    func arrivingOnTheCodeStepArmsTheClock() {
        let model = Self.model()

        model.advance()

        #expect(model.resendRemaining == 42)
        #expect(model.canResend == false)
    }

    @Test("The countdown reaches zero and opens the resend link")
    func countdownReachesZero() async {
        let model = Self.model(resendDelay: .seconds(3), resendTick: .milliseconds(1))
        model.advance()

        await model.countdown()

        #expect(model.resendRemaining == 0)
        #expect(model.canResend)
    }

    @Test("resend() re-arms the full delay and bumps the generation")
    func resendReArmsTheClock() async {
        let model = Self.model(resendDelay: .seconds(3), resendTick: .milliseconds(1))
        model.advance()
        let armed = model.resendGeneration
        await model.countdown()

        model.resend()

        #expect(model.resendRemaining == 3)
        #expect(model.resendGeneration > armed)
    }

    @Test("A cancelled verification stops the flow where it stands")
    func cancelledVerificationDoesNotAdvance() async {
        let model = Self.model(verifyDelay: .seconds(60))
        model.advance()
        model.setCode("699123")

        model.cancelVerification()

        #expect(model.isVerifying == false)
        #expect(model.verification == nil)
        #expect(model.step == .code)
    }

    @Test("A confirmed passcode is copied into the draft and advances to biometrics")
    func confirmedPasscodeAdvances() {
        let model = Self.model()
        for _ in 0..<2 { model.advance() }

        model.setPasscodeEntry("1234")
        model.setPasscodeEntry("1234")

        #expect(model.draft.passcode == "1234")
        #expect(model.step == .biometrics)
        #expect(model.lastPasscodeEvent == .confirmed)
    }

    @Test("Stale keyboard updates cannot confirm a passcode or skip biometrics")
    func staleCreationEditCannotConfirmPasscode() {
        let model = Self.model()
        for _ in 0..<2 { model.advance() }

        model.setPasscodeEntry("1234", during: .creating)
        model.setPasscodeEntry("1234", during: .creating)

        #expect(model.step == .passcode)
        #expect(model.passcode.phase == .confirming)
        #expect(model.passcode.entry.isEmpty)
        #expect(model.lastPasscodeEvent == nil)

        model.setPasscodeEntry("1234", during: .confirming)
        model.setPasscodeEntry("1234", during: .confirming)
        #expect(model.step == .biometrics)
    }

    @Test("Back from biometrics allows a new passcode without replaying the old confirmation")
    func backFromBiometricsRestartsPasscodeEntry() {
        let model = Self.model()
        for _ in 0..<2 { model.advance() }
        model.setPasscodeEntry("1234", during: .creating)
        model.setPasscodeEntry("1234", during: .confirming)

        model.back()
        model.setPasscodeEntry("1234", during: .confirming)

        #expect(model.step == .passcode)
        #expect(model.passcode == PasscodeEntry())
        #expect(model.draft.passcode.isEmpty)
        #expect(model.lastPasscodeEvent == nil)

        model.setPasscodeEntry("5678", during: .creating)
        #expect(model.step == .passcode)
        model.setPasscodeEntry("5678", during: .confirming)
        #expect(model.step == .biometrics)
        #expect(model.draft.passcode == "5678")

        model.back()
        model.back()
        #expect(model.step == .code)
    }

    @Test("A mismatch is reported once per attempt")
    func mismatchIsReportedOncePerAttempt() {
        let model = Self.model()
        for _ in 0..<2 { model.advance() }

        model.setPasscodeEntry("1234")
        model.setPasscodeEntry("9999")
        #expect(model.lastPasscodeEvent == .mismatch)

        model.setPasscodeEntry("1")
        #expect(model.lastPasscodeEvent == nil)
        #expect(model.step == .passcode)
    }

    @Test("chooseBiometrics(false) records the choice and still advances to the profile")
    func decliningBiometricsStillAdvances() {
        let model = Self.model()
        for _ in 0..<3 { model.advance() }

        model.chooseBiometrics(false)

        #expect(model.draft.biometricsEnabled == false)
        #expect(model.step == .profile)
    }

    @Test("submit() returns the local user when the server answers")
    func submitReturnsTheLocalUser() async {
        let model = Self.profileModel(result: .success(PreviewAccountClient.sampleUser))

        let user = await model.submit()

        #expect(model.submissionFailed == false)
        #expect(model.isSubmitting == false)
        #expect(user.phone == "+237699123456")
        #expect(user.fullName == "Aristide Mbassi")
    }

    @Test("submit() still returns the local user and flags the failure when the server is unreachable")
    func submitDegradesSilently() async {
        let model = Self.profileModel(result: .failure(.unreachable))

        let user = await model.submit()

        #expect(model.submissionFailed)
        #expect(user.phone == "+237699123456")
        #expect(user.kycVerified == false)
    }
}
