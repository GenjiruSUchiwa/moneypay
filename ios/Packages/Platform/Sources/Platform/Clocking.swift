import Foundation

public protocol Clocking: Sendable {
    var now: Date { get }
}

public struct SystemClock: Clocking {
    public init() {}
    public var now: Date { Date() }
}
