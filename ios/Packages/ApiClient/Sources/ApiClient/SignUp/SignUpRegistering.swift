import Foundation

public protocol SignUpRegistering: Sendable {
    func start(phone: String, termsVersion: String, privacyVersion: String) async throws(SignUpError) -> SignUpStarted
    func state(of signUp: SignUpStarted) async throws(SignUpError) -> SignUpState
    func resendCode(for signUp: SignUpStarted) async throws(SignUpError) -> SignUpState
    func verifyPhone(for signUp: SignUpStarted, code: String) async throws(SignUpError) -> PhoneVerified
}

extension ApiClient: SignUpRegistering {
    public func start(phone: String, termsVersion: String, privacyVersion: String) async throws(SignUpError) -> SignUpStarted {
        let body = JsonApiRequest(
            type: SignUpResourceType.signUps,
            attributes: StartSignUpAttributes(phone: phone, termsVersion: termsVersion, privacyVersion: privacyVersion)
        )
        let response: JsonApiResponse<StartedSignUpAttributes> = try await send(.post, "signups", body: body, failing: SignUpError.self)
        return SignUpStarted(response.data)
    }

    public func state(of signUp: SignUpStarted) async throws(SignUpError) -> SignUpState {
        let response: JsonApiResponse<SignUpStateAttributes> = try await send(
            .get, "signups/\(signUp.signUpId)", signUpToken: signUp.signUpToken, failing: SignUpError.self
        )
        return SignUpState(response.data)
    }

    public func resendCode(for signUp: SignUpStarted) async throws(SignUpError) -> SignUpState {
        let response: JsonApiResponse<SignUpStateAttributes> = try await send(
            .post, "signups/\(signUp.signUpId)/verification-code-deliveries", signUpToken: signUp.signUpToken, failing: SignUpError.self
        )
        return SignUpState(response.data)
    }

    public func verifyPhone(for signUp: SignUpStarted, code: String) async throws(SignUpError) -> PhoneVerified {
        let body = JsonApiRequest(
            type: SignUpResourceType.phoneVerifications,
            attributes: PhoneVerificationAttributes(verificationCode: code),
            relationships: [
                "signUp": JsonApiRelationship(data: JsonApiResourceIdentifier(type: SignUpResourceType.signUps, id: signUp.signUpId))
            ]
        )
        let response: JsonApiResponse<PhoneVerifiedAttributes> = try await send(
            .post, "signups/\(signUp.signUpId)/phone-verifications", signUpToken: signUp.signUpToken, body: body, failing: SignUpError.self
        )
        return PhoneVerified(response.data)
    }
}
