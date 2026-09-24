import Foundation
import Testing

@testable import ApiClient

@Suite("SessionManaging", .serialized)
struct SessionManagingTests {
    static let host = "sessions.test"
    static let accessToken = "access-token-under-test"
    static let bearer = "Bearer \(accessToken)"
    static let userId = "22961289-422c-4b91-a5eb-1365b1e7c67b"
    static let sessionId = "4cd89435-dcc2-45ce-804b-eafc1ad4b553"

    func makeClient() throws -> ApiClient {
        ApiClient(
            baseURL: try #require(URL(string: "https://\(Self.host)/")),
            session: StubURLProtocol.session(),
            clientVersion: "1.2.3"
        )
    }

    @Test("Refresh posts the documented session-refreshes document without a credential header")
    func refresh() async throws {
        StubURLProtocol.reset(host: Self.host, body: try fixture("session-credentials-response"))
        let client = try makeClient()

        let session = try await client.refresh(refreshToken: "<opaque-random-token>", deviceId: "1207158c-15fc-446d-a28a-702c564332ef")

        let exchange = try #require(StubURLProtocol.exchange(on: Self.host))
        let request = try #require(exchange.received)
        #expect(request.httpMethod == "POST")
        #expect(request.url?.path() == "/session-refreshes")
        #expect(request.value(forHTTPHeaderField: ApiClient.Header.authorization) == nil)
        #expect(request.value(forHTTPHeaderField: "Content-Type") == "application/vnd.api+json")
        let sent = try decode(JsonApiRequest<SessionRefreshAttributes>.self, from: try #require(exchange.receivedBody))
        let documented = try decode(JsonApiRequest<SessionRefreshAttributes>.self, from: try fixture("session-refresh-request"))
        #expect(sent == documented)

        #expect(session.sessionId == Self.sessionId)
        #expect(session.userId == Self.userId)
        #expect(session.accessToken == "<jwt>")
        #expect(session.refreshToken == "<rotated-opaque-random-token>")
        #expect(session.accessTokenExpiresAt == Self.date("2026-08-29T19:12:00Z"))
        #expect(session.refreshTokenExpiresAt == Self.date("2026-09-28T19:02:00Z"))
    }

    @Test("A session without a user relationship maps to decoding")
    func missingUser() async throws {
        let body = try fixture("session-credentials-response")
        var document = try #require(try JSONSerialization.jsonObject(with: body) as? [String: Any])
        var data = try #require(document["data"] as? [String: Any])
        data["relationships"] = nil
        document["data"] = data
        StubURLProtocol.reset(host: Self.host, body: try JSONSerialization.data(withJSONObject: document))
        let client = try makeClient()

        let error = await sessionError { try await client.refresh(refreshToken: "token", deviceId: "device") }

        #expect(error == .decoding)
    }

    @Test("Revoke deletes the current session with the Bearer credential and accepts an empty 204")
    func revoke() async throws {
        StubURLProtocol.reset(host: Self.host, status: 204)
        let client = try makeClient()

        try await client.revokeCurrentSession(accessToken: Self.accessToken)

        let request = try #require(StubURLProtocol.exchange(on: Self.host)?.received)
        #expect(request.httpMethod == "DELETE")
        #expect(request.url?.path() == "/sessions/current")
        #expect(request.value(forHTTPHeaderField: ApiClient.Header.authorization) == Self.bearer)
    }

    @Test("Current user gets users/me with the Bearer credential")
    func currentUser() async throws {
        StubURLProtocol.reset(host: Self.host, body: try fixture("current-user-response"))
        let client = try makeClient()

        let user = try await client.currentUser(accessToken: Self.accessToken)

        let request = try #require(StubURLProtocol.exchange(on: Self.host)?.received)
        #expect(request.httpMethod == "GET")
        #expect(request.url?.path() == "/users/me")
        #expect(request.value(forHTTPHeaderField: ApiClient.Header.authorization) == Self.bearer)
        #expect(user == CurrentUser(
            id: Self.userId,
            firstName: "Aristide",
            lastName: "Mbassi",
            phone: "237699123456",
            email: "aristide@example.cm",
            locale: "fr-CM",
            createdAt: try #require(Self.date("2026-08-29T19:02:00Z"))
        ))
    }

    @Test("Every documented problem type maps to its SessionError case", arguments: [
        ("session-invalid", 401, SessionError.sessionInvalid),
        ("refresh-token-reused", 401, .refreshTokenReused),
        ("validation", 422, .validation(["/data/attributes/refreshToken"])),
        ("rate-limited", 429, .rateLimited(retryAfter: .seconds(30)))
    ])
    func mapsProblemType(code: String, status: Int, expected: SessionError) async throws {
        let body = Self.problem(code: code, status: status)
        StubURLProtocol.reset(host: Self.host, status: status, headers: ["Retry-After": "30"], body: body)
        let client = try makeClient()

        let error = await sessionError { try await client.refresh(refreshToken: "token", deviceId: "device") }

        #expect(error == expected)
    }

    @Test("A sign-up problem type is unexpected on a session route")
    func foreignProblemType() async throws {
        StubURLProtocol.reset(host: Self.host, status: 401, body: Self.problem(code: "signup-token-invalid", status: 401))
        let client = try makeClient()

        let error = await sessionError { try await client.currentUser(accessToken: Self.accessToken) }

        guard case .unexpected(let problem?) = error else {
            Issue.record("Expected .unexpected, got \(String(describing: error))")
            return
        }
        #expect(problem.problemType == .signUpTokenInvalid)
    }

    @Test("A transport failure maps to unreachable")
    func transportFailure() async throws {
        StubURLProtocol.reset(host: Self.host, fails: true)
        let client = try makeClient()

        let error = await sessionError { try await client.revokeCurrentSession(accessToken: Self.accessToken) }

        #expect(error == .unreachable)
    }

    @Test("Session credentials never describe their tokens")
    func descriptionHidesTokens() {
        let rendered = [String(describing: SessionCredentials.sample), String(reflecting: SessionCredentials.sample)]

        for text in rendered {
            #expect(!text.contains(SessionCredentials.sample.accessToken))
            #expect(!text.contains(SessionCredentials.sample.refreshToken))
        }
    }

    private func sessionError<Value>(_ body: () async throws -> Value) async -> SessionError? {
        do {
            _ = try await body()
            return nil
        } catch {
            return error as? SessionError
        }
    }

    private func fixture(_ name: String) throws -> Data {
        let url = try #require(Bundle.module.url(forResource: name, withExtension: "json", subdirectory: "Fixtures"))
        return try Data(contentsOf: url)
    }

    private func decode<Value: Decodable>(_ type: Value.Type, from data: Data) throws -> Value {
        try JsonApiCoding.decoder.decode(type, from: data)
    }

    private static func date(_ iso: String) -> Date? {
        try? Date.ISO8601FormatStyle(includingFractionalSeconds: true).parse(iso)
    }

    private static func problem(code: String, status: Int) -> Data {
        Data("""
        {
          "type": "urn:monipay:error:\(code)",
          "title": "Problem",
          "status": \(status),
          "instance": "/session-refreshes",
          "traceId": "00-trace-01",
          "errors": [{ "detail": "Invalid.", "pointer": "/data/attributes/refreshToken" }]
        }
        """.utf8)
    }
}
