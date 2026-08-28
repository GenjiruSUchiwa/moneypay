import Foundation

/// Time as an injectable dependency. A balance, an authorization hold and an
/// OTP expiry are only testable when time does not come from `Date()`.
/// Named `Clocking`, not `Clock`: the Swift standard library already has one.
public protocol Clocking: Sendable {
    var now: Date { get }
}

public struct SystemClock: Clocking {
    public init() {}
    public var now: Date { Date() }
}
