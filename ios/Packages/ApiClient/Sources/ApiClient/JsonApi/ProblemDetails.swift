import Foundation

nonisolated public struct ProblemDetails: Codable, Sendable, Hashable {
    public struct ValidationItem: Codable, Sendable, Hashable {
        public let detail: String?
        public let pointer: String?
    }

    public let type: String
    public let status: Int
    public let title: String
    public let detail: String?
    public let instance: String?
    public let traceId: String?
    public let errors: [ValidationItem]?

    public var problemType: ProblemType {
        ProblemType(urn: type)
    }

    public var pointers: [String] {
        errors?.compactMap(\.pointer) ?? []
    }
}

nonisolated public struct ProblemType: Sendable, Hashable {
    public static let prefix = "urn:monipay:error:"

    public let urn: String

    public init(urn: String) {
        self.urn = urn
    }

    public init(code: String) {
        urn = Self.prefix + code
    }

    public var code: String {
        urn.hasPrefix(Self.prefix) ? String(urn.dropFirst(Self.prefix.count)) : urn
    }

    public static let validation = ProblemType(code: "validation")
    public static let rateLimited = ProblemType(code: "rate-limited")
    public static let concurrentModification = ProblemType(code: "concurrent-modification")
    public static let signUpStateInvalid = ProblemType(code: "signup-state-invalid")
    public static let signUpExpired = ProblemType(code: "signup-expired")
    public static let signUpTokenInvalid = ProblemType(code: "signup-token-invalid")
    public static let signUpAttemptLimit = ProblemType(code: "signup-attempt-limit")
    public static let signUpResendLimit = ProblemType(code: "signup-resend-limit")
    public static let signUpResendTooSoon = ProblemType(code: "signup-resend-too-soon")
    public static let verificationCodeInvalid = ProblemType(code: "verification-code-invalid")
    public static let verificationCodeExpired = ProblemType(code: "verification-code-expired")
    public static let verificationDeliveryUnavailable = ProblemType(code: "verification-delivery-unavailable")
    public static let phoneAlreadyRegistered = ProblemType(code: "phone-already-registered")
    public static let sessionInvalid = ProblemType(code: "session-invalid")
    public static let refreshTokenReused = ProblemType(code: "refresh-token-reused")
}
