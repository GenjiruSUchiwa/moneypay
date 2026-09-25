import Foundation
import Platform
import Security
import Synchronization

nonisolated public struct FixedClock: Clocking {
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

nonisolated public final class InMemoryCredentialStore: CredentialStoring {
    private let items: Mutex<[CredentialKey: Data]>

    public init(_ initial: [CredentialKey: Data] = [:]) {
        items = Mutex(initial)
    }

    public var contents: [CredentialKey: Data] { items.withLock { $0 } }

    public func read(_ key: CredentialKey) throws(CredentialStoreError) -> Data? {
        items.withLock { $0[key] }
    }

    public func write(_ data: Data, for key: CredentialKey) throws(CredentialStoreError) {
        items.withLock { $0[key] = data }
    }

    public func delete(_ key: CredentialKey) throws(CredentialStoreError) {
        _ = items.withLock { $0.removeValue(forKey: key) }
    }
}

nonisolated public struct FailingCredentialStore: CredentialStoring {
    private let error: CredentialStoreError

    public init(_ error: CredentialStoreError = .keychain(errSecInteractionNotAllowed)) {
        self.error = error
    }

    public func read(_ key: CredentialKey) throws(CredentialStoreError) -> Data? { throw error }
    public func write(_ data: Data, for key: CredentialKey) throws(CredentialStoreError) { throw error }
    public func delete(_ key: CredentialKey) throws(CredentialStoreError) { throw error }
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
