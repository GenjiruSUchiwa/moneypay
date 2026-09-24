import Foundation

public struct PreviewSessionClient: SessionManaging {
    private let refreshed: Result<SessionCredentials, SessionError>
    private let revoked: Result<Void, SessionError>
    private let user: Result<CurrentUser, SessionError>
    private let delay: Duration

    public init(
        refreshed: Result<SessionCredentials, SessionError> = .success(.sample),
        revoked: Result<Void, SessionError> = .success(()),
        user: Result<CurrentUser, SessionError> = .success(.sample),
        delay: Duration = .zero
    ) {
        self.refreshed = refreshed
        self.revoked = revoked
        self.user = user
        self.delay = delay
    }

    public func refresh(refreshToken: String, deviceId: String) async throws(SessionError) -> SessionCredentials {
        try await answer(refreshed)
    }

    public func revokeCurrentSession(accessToken: String) async throws(SessionError) {
        try await answer(revoked)
    }

    public func currentUser(accessToken: String) async throws(SessionError) -> CurrentUser {
        try await answer(user)
    }

    private func answer<Value>(_ result: Result<Value, SessionError>) async throws(SessionError) -> Value {
        if delay > .zero {
            try? await Task.sleep(for: delay)
        }
        return try result.get()
    }
}

public extension SessionCredentials {
    static let sample = SessionCredentials(
        sessionId: "4cd89435-dcc2-45ce-804b-eafc1ad4b553",
        userId: CurrentUser.sample.id,
        accessToken: "preview-access-token",
        accessTokenExpiresAt: .sample(minutes: 10),
        refreshToken: "preview-refresh-token",
        refreshTokenExpiresAt: .sample(minutes: 43_200)
    )
}

public extension CurrentUser {
    static let sample = CurrentUser(
        id: "22961289-422c-4b91-a5eb-1365b1e7c67b",
        firstName: "Aristide",
        lastName: "Mbassi",
        phone: "237699123456",
        email: "aristide@example.cm",
        locale: "fr-CM",
        createdAt: .sample(minutes: 0)
    )
}

private extension Date {
    static func sample(minutes: Int) -> Date {
        Date(timeIntervalSince1970: 1_788_030_000 + TimeInterval(minutes * 60))
    }
}
