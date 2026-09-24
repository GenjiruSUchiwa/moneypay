import Foundation
import Platform

public actor SessionStore {
    public enum RestoreOutcome: Sendable, Equatable {
        case noSession
        case restored(CurrentUser)
        case signedOut
        case unavailable(SessionStoreError)
    }

    private enum RefreshToken {
        case unread
        case known(String?)
    }

    private static let refreshMargin: TimeInterval = 30

    private let client: any SessionManaging
    private let credentials: any CredentialStoring
    private let clock: any Clocking
    private var refreshToken = RefreshToken.unread
    private var accessToken: (value: String, expiresAt: Date)?
    private var refreshing: Task<Result<SessionCredentials, SessionStoreError>, Never>?

    public init(client: any SessionManaging, credentials: any CredentialStoring, clock: any Clocking) {
        self.client = client
        self.credentials = credentials
        self.clock = clock
    }

    public func deviceId() throws(SessionStoreError) -> String {
        if let stored = try read(.deviceId) {
            return stored
        }
        let generated = UUID().uuidString
        try write(generated, for: .deviceId)
        return generated
    }

    public func store(_ session: SessionCredentials) throws(SessionStoreError) {
        adopt(session)
        try write(session.refreshToken, for: .refreshToken)
    }

    public func restore() async -> RestoreOutcome {
        do throws(SessionStoreError) {
            let token = try await refresh()
            do {
                return .restored(try await client.currentUser(accessToken: token))
            } catch {
                throw refused(error)
            }
        } catch .noSession {
            return .noSession
        } catch {
            return error.endsSession ? .signedOut : .unavailable(error)
        }
    }

    public func validAccessToken() async throws(SessionStoreError) -> String {
        if let accessToken, accessToken.expiresAt.timeIntervalSince(clock.now) > Self.refreshMargin {
            return accessToken.value
        }
        return try await refresh()
    }

    public func signOut() async throws(SessionStoreError) {
        if let token = try? await validAccessToken() {
            try? await client.revokeCurrentSession(accessToken: token)
        }
        try forget()
    }

    private func refresh() async throws(SessionStoreError) -> String {
        let task = refreshing ?? Task { await performRefresh() }
        refreshing = task
        let result = await task.value
        if refreshing == task {
            refreshing = nil
        }
        return try result.get().accessToken
    }

    private func performRefresh() async -> Result<SessionCredentials, SessionStoreError> {
        do throws(SessionStoreError) {
            let refreshToken = try currentRefreshToken()
            let deviceId = try deviceId()
            let session: SessionCredentials
            do {
                session = try await client.refresh(refreshToken: refreshToken, deviceId: deviceId)
            } catch {
                throw refused(error)
            }
            adopt(session)
            try? write(session.refreshToken, for: .refreshToken)
            return .success(session)
        } catch {
            return .failure(error)
        }
    }

    private func refused(_ error: SessionError) -> SessionStoreError {
        let failure = SessionStoreError.session(error)
        if failure.endsSession {
            try? forget()
        }
        return failure
    }

    private func currentRefreshToken() throws(SessionStoreError) -> String {
        if case .unread = refreshToken {
            refreshToken = .known(try read(.refreshToken))
        }
        guard case .known(let token?) = refreshToken else { throw .noSession }
        return token
    }

    private func adopt(_ session: SessionCredentials) {
        refreshToken = .known(session.refreshToken)
        accessToken = (session.accessToken, session.accessTokenExpiresAt)
    }

    private func forget() throws(SessionStoreError) {
        refreshToken = .known(nil)
        accessToken = nil
        do {
            try credentials.delete(.refreshToken)
        } catch {
            throw .credentialStorage(error)
        }
    }

    private func read(_ key: CredentialKey) throws(SessionStoreError) -> String? {
        do {
            return try credentials.read(key).flatMap { String(data: $0, encoding: .utf8) }
        } catch {
            throw .credentialStorage(error)
        }
    }

    private func write(_ value: String, for key: CredentialKey) throws(SessionStoreError) {
        do {
            try credentials.write(Data(value.utf8), for: key)
        } catch {
            throw .credentialStorage(error)
        }
    }
}
