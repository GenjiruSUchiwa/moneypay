import Foundation

protocol JsonApiFailure: Error {
    init(_ problem: ProblemDetails?, retryAfter: Duration?)
    static var unreachable: Self { get }
    static var decoding: Self { get }
}
