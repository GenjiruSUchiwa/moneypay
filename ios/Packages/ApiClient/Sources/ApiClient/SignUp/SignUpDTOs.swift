import Foundation

nonisolated public enum SignUpStatus: String, Codable, Sendable, Hashable {
    case codePending, phoneVerified, completed, locked, expired
}

nonisolated public enum CodeDelivery: String, Codable, Sendable, Hashable {
    case queued, sent, failed, expired
}

nonisolated public struct SignUpStarted: Sendable, Hashable {
    public let signUpId: String
    public let signUpToken: String
    public let codeDelivery: CodeDelivery
    public let codeExpiresAt: Date
    public let canResendAt: Date
    public let signUpExpiresAt: Date

    public init(
        signUpId: String,
        signUpToken: String,
        codeDelivery: CodeDelivery,
        codeExpiresAt: Date,
        canResendAt: Date,
        signUpExpiresAt: Date
    ) {
        self.signUpId = signUpId
        self.signUpToken = signUpToken
        self.codeDelivery = codeDelivery
        self.codeExpiresAt = codeExpiresAt
        self.canResendAt = canResendAt
        self.signUpExpiresAt = signUpExpiresAt
    }
}

nonisolated public struct SignUpState: Sendable, Hashable {
    public let signUpId: String
    public let status: SignUpStatus
    public let codeDelivery: CodeDelivery?
    public let codeExpiresAt: Date?
    public let canResendAt: Date
    public let signUpExpiresAt: Date

    public init(
        signUpId: String,
        status: SignUpStatus,
        codeDelivery: CodeDelivery?,
        codeExpiresAt: Date?,
        canResendAt: Date,
        signUpExpiresAt: Date
    ) {
        self.signUpId = signUpId
        self.status = status
        self.codeDelivery = codeDelivery
        self.codeExpiresAt = codeExpiresAt
        self.canResendAt = canResendAt
        self.signUpExpiresAt = signUpExpiresAt
    }
}

nonisolated public struct PhoneVerified: Sendable, Hashable {
    public let signUpId: String
    public let registrationToken: String
    public let signUpExpiresAt: Date

    public init(signUpId: String, registrationToken: String, signUpExpiresAt: Date) {
        self.signUpId = signUpId
        self.registrationToken = registrationToken
        self.signUpExpiresAt = signUpExpiresAt
    }
}

nonisolated enum SignUpResourceType {
    static let signUps = "signups"
    static let phoneVerifications = "phone-verifications"
}

nonisolated struct StartSignUpAttributes: Codable, Sendable, Hashable {
    let phone: String
    let termsVersion: String
    let privacyVersion: String
}

nonisolated struct StartedSignUpAttributes: Codable, Sendable, Hashable {
    let status: SignUpStatus
    let codeDelivery: CodeDelivery
    let signUpToken: String
    let codeExpiresAt: Date
    let canResendAt: Date
    let signUpExpiresAt: Date
}

nonisolated struct SignUpStateAttributes: Codable, Sendable, Hashable {
    let status: SignUpStatus
    let codeDelivery: CodeDelivery?
    let codeExpiresAt: Date?
    let canResendAt: Date
    let signUpExpiresAt: Date
}

nonisolated struct PhoneVerificationAttributes: Codable, Sendable, Hashable {
    let verificationCode: String
}

nonisolated struct PhoneVerifiedAttributes: Codable, Sendable, Hashable {
    let status: SignUpStatus
    let registrationToken: String
    let signUpExpiresAt: Date
}

extension SignUpStarted {
    init(_ resource: JsonApiResource<StartedSignUpAttributes>) {
        self.init(
            signUpId: resource.id,
            signUpToken: resource.attributes.signUpToken,
            codeDelivery: resource.attributes.codeDelivery,
            codeExpiresAt: resource.attributes.codeExpiresAt,
            canResendAt: resource.attributes.canResendAt,
            signUpExpiresAt: resource.attributes.signUpExpiresAt
        )
    }
}

extension SignUpState {
    init(_ resource: JsonApiResource<SignUpStateAttributes>) {
        self.init(
            signUpId: resource.id,
            status: resource.attributes.status,
            codeDelivery: resource.attributes.codeDelivery,
            codeExpiresAt: resource.attributes.codeExpiresAt,
            canResendAt: resource.attributes.canResendAt,
            signUpExpiresAt: resource.attributes.signUpExpiresAt
        )
    }
}

extension PhoneVerified {
    init(_ resource: JsonApiResource<PhoneVerifiedAttributes>) {
        self.init(
            signUpId: resource.id,
            registrationToken: resource.attributes.registrationToken,
            signUpExpiresAt: resource.attributes.signUpExpiresAt
        )
    }
}
