import Foundation

nonisolated public protocol Clocking: Sendable {
    var now: Date { get }
}

nonisolated public struct SystemClock: Clocking {
    public init() {}
    public var now: Date { Date() }
}
