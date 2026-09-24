import Foundation

nonisolated public enum SignUpError: Error, Sendable, Equatable {
    case validation([String])
    case rateLimited(retryAfter: Duration?)
    case signUpTokenInvalid
    case signUpStateInvalid
    case signUpExpired
    case verificationCodeInvalid
    case verificationCodeExpired
    case attemptLimit(retryAfter: Duration?)
    case resendLimit(retryAfter: Duration?)
    case resendTooSoon(retryAfter: Duration?)
    case phoneAlreadyRegistered
    case concurrentModification
    case deliveryUnavailable(retryAfter: Duration?)
    case unexpected(ProblemDetails?)
    case unreachable
    case decoding
}

extension SignUpError: JsonApiFailure {
    private static let plain: [ProblemType: SignUpError] = [
        .signUpTokenInvalid: .signUpTokenInvalid,
        .signUpStateInvalid: .signUpStateInvalid,
        .signUpExpired: .signUpExpired,
        .verificationCodeInvalid: .verificationCodeInvalid,
        .verificationCodeExpired: .verificationCodeExpired,
        .phoneAlreadyRegistered: .phoneAlreadyRegistered,
        .concurrentModification: .concurrentModification
    ]

    init(_ problem: ProblemDetails?, retryAfter: Duration?) {
        guard let problem else {
            self = .unexpected(nil)
            return
        }
        let type = problem.problemType
        self = switch type {
        case .validation: .validation(problem.pointers)
        case .rateLimited: .rateLimited(retryAfter: retryAfter)
        case .signUpAttemptLimit: .attemptLimit(retryAfter: retryAfter)
        case .signUpResendLimit: .resendLimit(retryAfter: retryAfter)
        case .signUpResendTooSoon: .resendTooSoon(retryAfter: retryAfter)
        case .verificationDeliveryUnavailable: .deliveryUnavailable(retryAfter: retryAfter)
        default: Self.plain[type] ?? .unexpected(problem)
        }
    }
}
