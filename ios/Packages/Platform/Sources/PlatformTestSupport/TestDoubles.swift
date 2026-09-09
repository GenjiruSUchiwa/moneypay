import Foundation
import Platform

public struct FixedClock: Clocking {
    public var now: Date

    public init(now: Date = Date(timeIntervalSince1970: 0)) {
        self.now = now
    }
}

public final class InMemoryKeyValueStoring: KeyValueStoring, @unchecked Sendable {
    private let lock = NSLock()
    private var storage: [String: Any] = [:]

    public init(_ initial: [String: Any] = [:]) { storage = initial }

    public func string(forKey key: String) -> String? {
        lock.withLock { storage[key] as? String }
    }

    public func bool(forKey key: String) -> Bool {
        lock.withLock { storage[key] as? Bool ?? false }
    }

    public func set(_ value: String?, forKey key: String) {
        lock.withLock { storage[key] = value }
    }

    public func set(_ value: Bool, forKey key: String) {
        lock.withLock { storage[key] = value }
    }
}

public final class RecordingLogging: Logging, @unchecked Sendable {
    private let lock = NSLock()
    private var entries: [(LogLevel, String)] = []

    public init() {}

    public func log(_ level: LogLevel, _ message: String) {
        lock.withLock { entries.append((level, message)) }
    }

    public var messages: [String] { lock.withLock { entries.map(\.1) } }
}
