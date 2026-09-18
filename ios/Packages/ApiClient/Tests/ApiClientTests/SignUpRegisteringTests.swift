import Foundation
import Testing

@testable import ApiClient

@Suite("SignUpRegistering", .serialized)
struct SignUpRegisteringTests {
    static let signUpId = "1f7d854a-3996-4a6b-8cc8-1fe3e83bc205"
    static let token = "signup-token-under-test"
    static let signUp = SignUpStarted(
        signUpId: signUpId,
        signUpToken: token,
        codeDelivery: .queued,
        codeExpiresAt: .distantFuture,
        canResendAt: .distantFuture,
        signUpExpiresAt: .distantFuture
    )

    func makeClient() throws -> ApiClient {
        ApiClient(
            baseURL: try #require(URL(string: "https://api.test/")),
            session: StubURLProtocol.session(),
            clientVersion: "1.2.3"
        )
    }

    @Test("Start posts the documented signups document and decodes the started resource")
    func start() async throws {
        StubURLProtocol.reset(status: 202, body: try fixture("start-signup-response"))
        let client = try makeClient()

        let started = try await client.start(phone: "237699123456", termsVersion: "terms-2026-08", privacyVersion: "privacy-2026-08")

        let request = try #require(StubURLProtocol.received)
        #expect(request.httpMethod == "POST")
        #expect(request.url?.path() == "/signups")
        #expect(request.value(forHTTPHeaderField: "Accept") == "application/vnd.api+json, application/problem+json")
        #expect(request.value(forHTTPHeaderField: "Content-Type") == "application/vnd.api+json")
        #expect(request.value(forHTTPHeaderField: "X-MoniPay-Client") == "1.2.3")
        #expect(request.value(forHTTPHeaderField: "Authorization") == nil)
        let sent = try decode(JsonApiRequest<StartSignUpAttributes>.self, from: try #require(StubURLProtocol.receivedBody))
        let documented = try decode(JsonApiRequest<StartSignUpAttributes>.self, from: try fixture("start-signup-request"))
        #expect(sent == documented)

        #expect(started.signUpId == Self.signUpId)
        #expect(started.signUpToken == "<opaque-random-token>")
        #expect(started.codeDelivery == .queued)
        #expect(started.codeExpiresAt == Self.date("2026-08-29T19:05:00Z"))
        #expect(started.canResendAt == Self.date("2026-08-29T19:01:00Z"))
        #expect(started.signUpExpiresAt == Self.date("2026-08-29T19:15:00Z"))
    }

    @Test("State gets the sign-up with the SignUp credential and decodes fractional timestamps")
    func state() async throws {
        StubURLProtocol.reset(body: try fixture("get-signup-response"))
        let client = try makeClient()

        let state = try await client.state(of: Self.signUp)

        let request = try #require(StubURLProtocol.received)
        #expect(request.httpMethod == "GET")
        #expect(request.url?.path() == "/signups/\(Self.signUpId)")
        #expect(request.value(forHTTPHeaderField: "Authorization") == "SignUp \(Self.token)")
        #expect(request.value(forHTTPHeaderField: "Content-Type") == nil)
        #expect(state.status == .codePending)
        #expect(state.codeDelivery == .sent)
        #expect(state.codeExpiresAt == Self.date("2026-08-29T19:05:00.1234567Z"))
    }

    @Test("Resend posts an empty body to the deliveries route and decodes the refreshed state")
    func resend() async throws {
        StubURLProtocol.reset(status: 202, body: try fixture("resend-code-response"))
        let client = try makeClient()

        let state = try await client.resendCode(for: Self.signUp)

        let request = try #require(StubURLProtocol.received)
        #expect(request.httpMethod == "POST")
        #expect(request.url?.path() == "/signups/\(Self.signUpId)/verification-code-deliveries")
        #expect(request.value(forHTTPHeaderField: "Authorization") == "SignUp \(Self.token)")
        #expect(StubURLProtocol.receivedBody.map(\.isEmpty) ?? true)
        #expect(state.codeDelivery == .queued)
        #expect(state.canResendAt == Self.date("2026-08-29T19:03:00Z"))
    }

    @Test("Verify posts the documented phone-verifications document and decodes the registration token")
    func verify() async throws {
        StubURLProtocol.reset(body: try fixture("verify-phone-response"))
        let client = try makeClient()

        let verified = try await client.verifyPhone(for: Self.signUp, code: "123456")

        let request = try #require(StubURLProtocol.received)
        #expect(request.url?.path() == "/signups/\(Self.signUpId)/phone-verifications")
        let sent = try decode(JsonApiRequest<PhoneVerificationAttributes>.self, from: try #require(StubURLProtocol.receivedBody))
        let documented = try decode(JsonApiRequest<PhoneVerificationAttributes>.self, from: try fixture("verify-phone-request"))
        #expect(sent == documented)
        #expect(verified.signUpId == Self.signUpId)
        #expect(verified.registrationToken == "<opaque-random-token>")
    }

    @Test("A documented problem decodes to ProblemDetails with its pointers")
    func problemDetails() async throws {
        StubURLProtocol.reset(status: 422, body: try fixture("problem-verification-code-invalid"))
        let client = try makeClient()

        let error = await signUpError { try await client.verifyPhone(for: Self.signUp, code: "000000") }

        #expect(error == .verificationCodeInvalid)
        let problem = try decode(ProblemDetails.self, from: try fixture("problem-verification-code-invalid"))
        #expect(problem.problemType == .verificationCodeInvalid)
        #expect(problem.status == 422)
        #expect(problem.traceId == "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01")
        #expect(problem.pointers == ["/data/attributes/verificationCode"])
    }

    @Test("Every documented problem type maps to its SignUpError case", arguments: [
        ("validation", 422, SignUpError.validation(["/data/attributes/phone"])),
        ("rate-limited", 429, .rateLimited(retryAfter: .seconds(30))),
        ("signup-token-invalid", 401, .signUpTokenInvalid),
        ("signup-state-invalid", 409, .signUpStateInvalid),
        ("signup-expired", 410, .signUpExpired),
        ("verification-code-invalid", 422, .verificationCodeInvalid),
        ("verification-code-expired", 410, .verificationCodeExpired),
        ("signup-attempt-limit", 429, .attemptLimit(retryAfter: .seconds(30))),
        ("signup-resend-limit", 429, .resendLimit(retryAfter: .seconds(30))),
        ("signup-resend-too-soon", 429, .resendTooSoon(retryAfter: .seconds(30))),
        ("phone-already-registered", 409, .phoneAlreadyRegistered),
        ("concurrent-modification", 409, .concurrentModification),
        ("verification-delivery-unavailable", 503, .deliveryUnavailable(retryAfter: .seconds(30)))
    ])
    func mapsProblemType(code: String, status: Int, expected: SignUpError) async throws {
        StubURLProtocol.reset(status: status, headers: ["Retry-After": "30"], body: Self.problem(code: code, status: status))
        let client = try makeClient()

        let error = await signUpError { try await client.state(of: Self.signUp) }

        #expect(error == expected)
    }

    @Test("An unknown problem type maps to unexpected with the decoded problem")
    func unknownProblemType() async throws {
        StubURLProtocol.reset(status: 418, body: Self.problem(code: "teapot", status: 418))
        let client = try makeClient()

        let error = await signUpError { try await client.state(of: Self.signUp) }

        guard case .unexpected(let problem?) = error else {
            Issue.record("Expected .unexpected, got \(String(describing: error))")
            return
        }
                #expect(problem.problemType.code == "teapot")
    }

    @Test("A problem type outside the MoniPay namespace is unknown as a whole")
    func foreignProblemType() throws {
        let problem = try decode(ProblemDetails.self, from: Data(#"{"type":"about:blank","title":"Nope","status":400}"#.utf8))
        #expect(problem.problemType.code == "about:blank")
        #expect(problem.problemType.urn == "about:blank")
    }

    @Test("A non-problem failure body maps to unexpected without details")
    func nonProblemBody() async throws {
        StubURLProtocol.reset(status: 502, body: Data("<html>Bad Gateway</html>".utf8))
        let client = try makeClient()

        let error = await signUpError { try await client.state(of: Self.signUp) }

        #expect(error == SignUpError.unexpected(nil))
    }

    @Test("A transport failure maps to unreachable")
    func transportFailure() async throws {
        StubURLProtocol.reset(fails: true)
        let client = try makeClient()

        let error = await signUpError { try await client.state(of: Self.signUp) }

        #expect(error == .unreachable)
    }

    @Test("A malformed success body maps to decoding")
    func malformedSuccess() async throws {
        StubURLProtocol.reset(body: Data(#"{"data":{"type":"signups"}}"#.utf8))
        let client = try makeClient()

        let error = await signUpError { try await client.state(of: Self.signUp) }

        #expect(error == .decoding)
    }

    private func signUpError<Value>(_ body: () async throws -> Value) async -> SignUpError? {
        do {
            _ = try await body()
            return nil
        } catch {
            return error as? SignUpError
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
          "instance": "/signups/\(signUpId)",
          "traceId": "00-trace-01",
          "errors": [{ "detail": "Invalid.", "pointer": "/data/attributes/phone" }]
        }
        """.utf8)
    }
}
