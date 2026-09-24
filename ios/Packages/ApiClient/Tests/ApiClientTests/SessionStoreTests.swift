import Foundation
import Platform
import PlatformTestSupport
import Testing

@testable import ApiClient

@Suite("SessionStore")
struct SessionStoreTests {
    static let now = Date(timeIntervalSince1970: 1_788_030_000)
    static let deviceId = "1207158c-15fc-446d-a28a-702c564332ef"

    @MainActor final class ScriptedClient: SessionManaging {
        var refusal: SessionError?
        var accessTokenLifetime: TimeInterval = 600
        private(set) var refreshCalls: [(refreshToken: String, deviceId: String)] = []
        private(set) var revokedTokens: [String] = []

        func refresh(refreshToken: String, deviceId: String) async throws(SessionError) -> SessionCredentials {
            refreshCalls.append((refreshToken, deviceId))
            await Task.yield()
            if let refusal { throw refusal }
            let generation = refreshCalls.count
            return SessionStoreTests.credentials(
                access: "access-\(generation)",
                refresh: "refresh-\(generation)",
                expiresIn: accessTokenLifetime
            )
        }

        func revokeCurrentSession(accessToken: String) async throws(SessionError) {
            revokedTokens.append(accessToken)
        }

        func currentUser(accessToken: String) async throws(SessionError) -> CurrentUser { .sample }
    }

    nonisolated struct PartlyFailingCredentialStore: CredentialStoring {
        enum Operation { case write, delete }

        static let failure = CredentialStoreError.keychain(-25_308)

        let backing: InMemoryCredentialStore
        let failing: Operation

        func read(_ key: CredentialKey) throws(CredentialStoreError) -> Data? {
            try backing.read(key)
        }

        func write(_ data: Data, for key: CredentialKey) throws(CredentialStoreError) {
            guard failing != .write else { throw Self.failure }
            try backing.write(data, for: key)
        }

        func delete(_ key: CredentialKey) throws(CredentialStoreError) {
            guard failing != .delete else { throw Self.failure }
            try backing.delete(key)
        }
    }

    let client = ScriptedClient()

    func makeStore(_ credentials: any CredentialStoring) -> SessionStore {
        SessionStore(client: client, credentials: credentials, clock: FixedClock(now: Self.now))
    }

    static func credentials(access: String, refresh: String, expiresIn lifetime: TimeInterval) -> SessionCredentials {
        SessionCredentials(
            sessionId: SessionCredentials.sample.sessionId,
            userId: CurrentUser.sample.id,
            accessToken: access,
            accessTokenExpiresAt: now.addingTimeInterval(lifetime),
            refreshToken: refresh,
            refreshTokenExpiresAt: now.addingTimeInterval(2_592_000)
        )
    }

    static func stored(refreshToken: String? = "stored-refresh", deviceId: String? = deviceId) -> InMemoryCredentialStore {
        var items: [CredentialKey: Data] = [:]
        items[.refreshToken] = refreshToken.map { Data($0.utf8) }
        items[.deviceId] = deviceId.map { Data($0.utf8) }
        return InMemoryCredentialStore(items)
    }

    @Test("Restore without a stored refresh token reports no session and sends no request")
    func restoreWithoutSession() async {
        let store = makeStore(Self.stored(refreshToken: nil))

        #expect(await store.restore() == .noSession)
        #expect(client.refreshCalls.isEmpty)
    }

    @Test("Restore refreshes with the stored token, rotates it, then loads the current user")
    func restoreRotates() async throws {
        let credentials = Self.stored()
        let store = makeStore(credentials)

        #expect(await store.restore() == .restored(.sample))

        #expect(client.refreshCalls.map(\.refreshToken) == ["stored-refresh"])
        #expect(client.refreshCalls.map(\.deviceId) == [Self.deviceId])
        #expect(credentials.contents[.refreshToken] == Data("refresh-1".utf8))
        #expect(try await store.validAccessToken() == "access-1")
        #expect(client.refreshCalls.count == 1)
    }

    @Test("A refused refresh clears the refresh token, keeps the device, and signs out",
          arguments: [SessionError.sessionInvalid, .refreshTokenReused])
    func refusalSignsOut(refusal: SessionError) async {
        let credentials = Self.stored()
        client.refusal = refusal
        let store = makeStore(credentials)

        #expect(await store.restore() == .signedOut)

        #expect(credentials.contents[.refreshToken] == nil)
        #expect(credentials.contents[.deviceId] == Data(Self.deviceId.utf8))
    }

    @Test("An unreachable backend keeps the stored session")
    func unreachableKeepsSession() async {
        let credentials = Self.stored()
        client.refusal = .unreachable
        let store = makeStore(credentials)

        #expect(await store.restore() == .unavailable(.session(.unreachable)))
        #expect(credentials.contents[.refreshToken] == Data("stored-refresh".utf8))
    }

    @Test("A credential store failure is reported without a request")
    func credentialStoreFailure() async {
        let failure = CredentialStoreError.keychain(-25_308)
        let store = makeStore(FailingCredentialStore(failure))

        #expect(await store.restore() == .unavailable(.credentialStorage(failure)))
        #expect(client.refreshCalls.isEmpty)
    }

    @Test("The device identifier stays the same across restores")
    func deviceIdStable() async throws {
        let credentials = Self.stored()
        let store = makeStore(credentials)

        _ = await store.restore()
        _ = await store.restore()

        #expect(client.refreshCalls.map(\.deviceId) == [Self.deviceId, Self.deviceId])
        #expect(try await store.deviceId() == Self.deviceId)
    }

    @Test("A missing device identifier is generated once and stored")
    func deviceIdGenerated() async throws {
        let credentials = Self.stored(deviceId: nil)
        let store = makeStore(credentials)

        _ = await store.restore()
        _ = await store.restore()

        let stored = try #require(credentials.contents[.deviceId].flatMap { String(data: $0, encoding: .utf8) })
        #expect(UUID(uuidString: stored) != nil)
        #expect(client.refreshCalls.map(\.deviceId) == [stored, stored])
    }

    @Test("A token that outlives the refresh margin is returned without a refresh")
    func freshTokenReused() async throws {
        let store = makeStore(Self.stored())
        try await store.store(Self.credentials(access: "stored-access", refresh: "stored-refresh", expiresIn: 31))

        #expect(try await store.validAccessToken() == "stored-access")
        #expect(client.refreshCalls.isEmpty)
    }

    @Test("A token that expires within 30 seconds is refreshed")
    func expiringTokenRefreshed() async throws {
        let credentials = InMemoryCredentialStore()
        let store = makeStore(credentials)
        try await store.store(Self.credentials(access: "stored-access", refresh: "completion-refresh", expiresIn: 30))

        #expect(try await store.validAccessToken() == "access-1")
        #expect(client.refreshCalls.map(\.refreshToken) == ["completion-refresh"])
        #expect(credentials.contents[.refreshToken] == Data("refresh-1".utf8))
    }

    @Test("Concurrent callers share a single refresh")
    func concurrentCallersRefreshOnce() async {
        let store = makeStore(Self.stored())

        let tokens = await withTaskGroup(of: String?.self) { group in
            for _ in 0..<8 {
                group.addTask { try? await store.validAccessToken() }
            }
            var tokens: [String?] = []
            for await token in group {
                tokens.append(token)
            }
            return tokens
        }

        #expect(client.refreshCalls.count == 1)
        #expect(Set(tokens) == ["access-1"])
    }

    @Test("Without a stored refresh token, a valid access token is refused")
    func noRefreshTokenRefused() async {
        let store = makeStore(Self.stored(refreshToken: nil))

        await #expect(throws: SessionStoreError.noSession) { try await store.validAccessToken() }
    }

    @Test("A rotated refresh token that cannot be saved is still used for the next refresh")
    func unsavedRotationKept() async throws {
        client.accessTokenLifetime = 0
        let store = makeStore(PartlyFailingCredentialStore(backing: Self.stored(), failing: .write))

        #expect(try await store.validAccessToken() == "access-1")
        #expect(try await store.validAccessToken() == "access-2")

        #expect(client.refreshCalls.map(\.refreshToken) == ["stored-refresh", "refresh-1"])
    }

    @Test("Sign out revokes the current session and clears the refresh token")
    func signOut() async throws {
        let credentials = Self.stored()
        let store = makeStore(credentials)
        try await store.store(Self.credentials(access: "stored-access", refresh: "stored-refresh", expiresIn: 600))

        try await store.signOut()

        #expect(client.revokedTokens == ["stored-access"])
        #expect(credentials.contents[.refreshToken] == nil)
        await #expect(throws: SessionStoreError.noSession) { try await store.validAccessToken() }
    }

    @Test("Sign out reports a refresh token it could not delete and still forgets the session")
    func signOutDeleteFailure() async throws {
        let store = makeStore(PartlyFailingCredentialStore(backing: Self.stored(), failing: .delete))
        try await store.store(Self.credentials(access: "stored-access", refresh: "stored-refresh", expiresIn: 600))

        await #expect(throws: SessionStoreError.credentialStorage(PartlyFailingCredentialStore.failure)) {
            try await store.signOut()
        }
        await #expect(throws: SessionStoreError.noSession) { try await store.validAccessToken() }
    }
}
