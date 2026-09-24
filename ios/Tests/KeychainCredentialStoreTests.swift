import Foundation
import Platform
import Security
import Testing

@Suite("Keychain credential store", .serialized)
struct KeychainCredentialStoreTests {
    static let service = "com.monipay.app.tests.credentials"

    let store = KeychainCredentialStore(service: Self.service)

    @Test("A credential is written, overwritten, read back and deleted")
    func roundTrip() throws {
        try store.delete(.refreshToken)
        #expect(try store.read(.refreshToken) == nil)

        try store.write(Data("first".utf8), for: .refreshToken)
        try store.write(Data("second".utf8), for: .refreshToken)
        #expect(try store.read(.refreshToken) == Data("second".utf8))

        try store.delete(.refreshToken)
        #expect(try store.read(.refreshToken) == nil)
    }

    @Test("A credential is device-only and never synchronized")
    func deviceOnly() throws {
        try store.write(Data("device".utf8), for: .deviceId)
        defer { try? store.delete(.deviceId) }

        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: Self.service,
            kSecAttrAccount as String: CredentialKey.deviceId.rawValue,
            kSecReturnAttributes as String: true
        ]
        var result: CFTypeRef?
        #expect(SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess)
        let attributes = try #require(result as? [String: Any])
        #expect(attributes[kSecAttrAccessible as String] as? String == kSecAttrAccessibleWhenUnlockedThisDeviceOnly as String)
        #expect((attributes[kSecAttrSynchronizable as String] as? Bool ?? false) == false)
    }
}
