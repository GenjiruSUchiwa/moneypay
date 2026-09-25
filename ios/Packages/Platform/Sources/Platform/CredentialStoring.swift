import Foundation

nonisolated public enum CredentialKey: String, Sendable {
    case refreshToken
    case deviceId
}

nonisolated public enum CredentialStoreError: Error, Sendable, Equatable {
    case keychain(OSStatus)
}

nonisolated public protocol CredentialStoring: Sendable {
    func read(_ key: CredentialKey) throws(CredentialStoreError) -> Data?
    func write(_ data: Data, for key: CredentialKey) throws(CredentialStoreError)
    func delete(_ key: CredentialKey) throws(CredentialStoreError)
}
