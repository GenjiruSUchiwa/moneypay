import Foundation

nonisolated public struct SessionCredentials: Sendable, Hashable, CustomStringConvertible {
    public let sessionId: String
    public let userId: String
    public let accessToken: String
    public let accessTokenExpiresAt: Date
    public let refreshToken: String
    public let refreshTokenExpiresAt: Date

    public init(
        sessionId: String,
        userId: String,
        accessToken: String,
        accessTokenExpiresAt: Date,
        refreshToken: String,
        refreshTokenExpiresAt: Date
    ) {
        self.sessionId = sessionId
        self.userId = userId
        self.accessToken = accessToken
        self.accessTokenExpiresAt = accessTokenExpiresAt
        self.refreshToken = refreshToken
        self.refreshTokenExpiresAt = refreshTokenExpiresAt
    }

    public var description: String {
        "SessionCredentials(sessionId: \(sessionId), userId: \(userId), accessTokenExpiresAt: \(accessTokenExpiresAt))"
    }
}

nonisolated public struct CurrentUser: Sendable, Hashable {
    public let id: String
    public let firstName: String
    public let lastName: String
    public let phone: String
    public let email: String
    public let locale: String
    public let createdAt: Date

    public init(id: String, firstName: String, lastName: String, phone: String, email: String, locale: String, createdAt: Date) {
        self.id = id
        self.firstName = firstName
        self.lastName = lastName
        self.phone = phone
        self.email = email
        self.locale = locale
        self.createdAt = createdAt
    }
}

nonisolated enum SessionResourceType {
    static let sessionRefreshes = "session-refreshes"
}

nonisolated enum SessionRelationship {
    static let user = "user"
}

nonisolated struct SessionRefreshAttributes: Codable, Sendable, Hashable {
    let refreshToken: String
    let deviceId: String
}

nonisolated struct SessionCredentialsAttributes: Codable, Sendable, Hashable {
    let accessToken: String
    let accessTokenExpiresAt: Date
    let refreshToken: String
    let refreshTokenExpiresAt: Date
}

nonisolated struct UserAttributes: Codable, Sendable, Hashable {
    let firstName: String
    let lastName: String
    let phone: String
    let email: String
    let locale: String
    let createdAt: Date
}

extension SessionCredentials {
    init(_ resource: JsonApiResource<SessionCredentialsAttributes>) throws(SessionError) {
        guard let userId = resource.relatedId(SessionRelationship.user) else { throw .decoding }
        self.init(
            sessionId: resource.id,
            userId: userId,
            accessToken: resource.attributes.accessToken,
            accessTokenExpiresAt: resource.attributes.accessTokenExpiresAt,
            refreshToken: resource.attributes.refreshToken,
            refreshTokenExpiresAt: resource.attributes.refreshTokenExpiresAt
        )
    }
}

extension CurrentUser {
    init(_ resource: JsonApiResource<UserAttributes>) {
        self.init(
            id: resource.id,
            firstName: resource.attributes.firstName,
            lastName: resource.attributes.lastName,
            phone: resource.attributes.phone,
            email: resource.attributes.email,
            locale: resource.attributes.locale,
            createdAt: resource.attributes.createdAt
        )
    }
}
