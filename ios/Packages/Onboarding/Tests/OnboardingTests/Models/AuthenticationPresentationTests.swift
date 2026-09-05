import Testing
@testable import Onboarding

@Suite("Authentication presentation")
struct AuthenticationPresentationTests {
    @Test("Unavailable verification cannot accept a code", arguments: [
        OnboardingRoot.PreviewScenario.queued, .verifying, .deliveryFailed, .locked,
        .expiredCode, .accountExists, .noAccount
    ])
    func unavailableVerification(scenario: OnboardingRoot.PreviewScenario) throws {
        let feedback = try #require(scenario.feedback)
        #expect(!feedback.allowsCodeEntry)
        #expect(scenario.step == .code)
    }

    @Test("A resend refusal does not block the code already received", arguments: [
        OnboardingRoot.PreviewScenario.invalidCode, .resendTooSoon, .resendLimit
    ])
    func existingCodeRemainsUsable(scenario: OnboardingRoot.PreviewScenario) throws {
        let feedback = try #require(scenario.feedback)
        #expect(feedback.allowsCodeEntry)
        #expect(feedback.actionTitle == nil)
    }
}
