import Foundation

nonisolated public enum SessionError: Error, Sendable, Equatable {
    case sessionInvalid
    case refreshTokenReused
    case validation([String])
    case rateLimited(retryAfter: Duration?)
    case unexpected(ProblemDetails?)
    case unreachable
    case decoding
}

extension SessionError: JsonApiFailure {
    init(_ problem: ProblemDetails?, retryAfter: Duration?) {
        guard let problem else {
            self = .unexpected(nil)
            return
        }
        self = switch problem.problemType {
        case .sessionInvalid: .sessionInvalid
        case .refreshTokenReused: .refreshTokenReused
        case .validation: .validation(problem.pointers)
        case .rateLimited: .rateLimited(retryAfter: retryAfter)
        default: .unexpected(problem)
        }
    }
}
