import Foundation
import Testing

@testable import ApiClient

@Suite("PreviewSignUpClient")
struct PreviewSignUpClientTests {
    @Test("The default client answers every route with its sample")
    func answersWithSamples() async throws {
        let client = PreviewSignUpClient()

        let started = try await client.start(phone: "237699123456", termsVersion: "t", privacyVersion: "p")
        let state = try await client.state(of: started)
        let resent = try await client.resendCode(for: started)
        let verified = try await client.verifyPhone(for: started, code: "123456")

        #expect(started == .sample)
        #expect(state == .sample)
        #expect(resent == .sample)
        #expect(verified == .sample)
    }

    @Test("A scripted failure is thrown as the typed error")
    func scriptedFailure() async {
        let client = PreviewSignUpClient(verified: .failure(.verificationCodeInvalid), delay: .milliseconds(1))

        do {
            _ = try await client.verifyPhone(for: .sample, code: "000000")
            Issue.record("Expected a failure")
        } catch {
            #expect(error == .verificationCodeInvalid)
        }
    }
}
