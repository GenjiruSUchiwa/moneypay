import Foundation

public protocol SessionManaging: Sendable {
    func refresh(refreshToken: String, deviceId: String) async throws(SessionError) -> SessionCredentials
    func currentUser(accessToken: String) async throws(SessionError) -> CurrentUser
    func revokeCurrentSession(accessToken: String) async throws(SessionError)
}

extension ApiClient: SessionManaging {
    public func refresh(refreshToken: String, deviceId: String) async throws(SessionError) -> SessionCredentials {
        let body = JsonApiRequest(
            type: SessionResourceType.sessionRefreshes,
            attributes: SessionRefreshAttributes(refreshToken: refreshToken, deviceId: deviceId)
        )
        let response: JsonApiResponse<SessionCredentialsAttributes> = try await send(
            .post, "session-refreshes", body: body, failing: SessionError.self
        )
        return try SessionCredentials(response.data)
    }

    public func currentUser(accessToken: String) async throws(SessionError) -> CurrentUser {
        let response: JsonApiResponse<UserAttributes> = try await send(
            .get, "users/me", credential: .bearer(accessToken), failing: SessionError.self
        )
        return CurrentUser(response.data)
    }

    public func revokeCurrentSession(accessToken: String) async throws(SessionError) {
        try await exchange(.delete, "sessions/current", credential: .bearer(accessToken), failing: SessionError.self)
    }
}
