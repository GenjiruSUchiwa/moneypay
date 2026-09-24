import Foundation
import Security

nonisolated public struct KeychainCredentialStore: CredentialStoring {
    public static let defaultService = "com.monipay.app.credentials"

    private let service: String

    public init(service: String = Self.defaultService) {
        self.service = service
    }

    public func read(_ key: CredentialKey) throws(CredentialStoreError) -> Data? {
        var query = identity(of: key)
        query[kSecReturnData as String] = true
        query[kSecMatchLimit as String] = kSecMatchLimitOne
        var result: CFTypeRef?
        let status = SecItemCopyMatching(query as CFDictionary, &result)
        switch status {
        case errSecSuccess: return result as? Data
        case errSecItemNotFound: return nil
        default: throw .keychain(status)
        }
    }

    public func write(_ data: Data, for key: CredentialKey) throws(CredentialStoreError) {
        let attributes: [String: Any] = [
            kSecValueData as String: data,
            kSecAttrAccessible as String: kSecAttrAccessibleWhenUnlockedThisDeviceOnly
        ]
        let updated = SecItemUpdate(identity(of: key) as CFDictionary, attributes as CFDictionary)
        switch updated {
        case errSecSuccess:
            return
        case errSecItemNotFound:
            let item = identity(of: key).merging(attributes) { $1 }
            let added = SecItemAdd(item as CFDictionary, nil)
            guard added == errSecSuccess else { throw .keychain(added) }
        default:
            throw .keychain(updated)
        }
    }

    public func delete(_ key: CredentialKey) throws(CredentialStoreError) {
        let status = SecItemDelete(identity(of: key) as CFDictionary)
        guard status == errSecSuccess || status == errSecItemNotFound else { throw .keychain(status) }
    }

    private func identity(of key: CredentialKey) -> [String: Any] {
        [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key.rawValue,
            kSecAttrSynchronizable as String: false
        ]
    }
}
